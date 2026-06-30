using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using power_plant_coding_challenge_core.Options;
using power_plant_coding_challenge_domain.Enums;
using power_plant_coding_challenge_domain.Models;

namespace power_plant_coding_challenge_core.Features.CalculateProductionPlan;

public static partial class CalculateProductionPlan
{
    public class Handler(IOptions<ProductionPlanOptions> options) : IRequestHandler<Command, Response>
    {
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var orderedResults = new List<Result>();
            var includeCo2Costs = options.Value.IncludeCo2Costs;
            var results = new List<Result>();
            List<Result>? resultToAddToWindPowerplants = null;
            var useWind = false; //flag used for ordering the final result list.

            //Step 1.A : Order the powerplants by cost efficiency, then by Pmax descending. This is to ensure that we use the most efficient powerplants first, and in case of a tie, we use the one with the highest Pmax.
            var orderedPowerplants = request.Powerplants
                .OrderBy(powerplant => powerplant.GetCostEfficiency(request.Fuels, includeCo2Costs))
                .ThenByDescending(powerplant => powerplant.Pmax)
                .ToList();

            //Step 1.B : If the load is 0, return a list of results with all powerplants set to 0 production.
            if (request.Load == 0)
            {
                orderedResults.AddRange(orderedPowerplants.Select(powerplant => new Result(powerplant.Name, 0m)));

                return new(orderedResults);
            }

            //Step 2 : Split the merit-order list into two lists: one for wind turbines and one for non-wind turbines. This is to ensure that when we use windtrubines, we use them as a non-throttleable powerplant.
            var windOnlyPowerplants = orderedPowerplants
                .Where(powerplant => powerplant.Type == PowerplantType.Windturbine)
                .ToList();

            var nonWindPowerplants = orderedPowerplants
                .Where(powerplant => powerplant.Type != PowerplantType.Windturbine)
                .ToList();

            //Step 3 : Calculate the total wind power available to check if it can meet the load.
            var totalWindPower = windOnlyPowerplants.Sum(powerplant => powerplant.GetActualPMax(request.Fuels));
            var loadAfterWindPower = request.Load - totalWindPower;

            //the wind power can meet the load exactly, we use it and set the non-wind powerplants to 0.
            if (loadAfterWindPower == 0) resultToAddToWindPowerplants = new List<Result>();

            //the wind power can meet part of the load, we try to find a combination of non-wind powerplants that can meet the remaining load.
            else if (loadAfterWindPower > 0) 
                resultToAddToWindPowerplants = GetUsablePowerplantsFromList(nonWindPowerplants, request.Fuels, loadAfterWindPower);

            var resultsWithoutWindPowerplants = GetUsablePowerplantsFromList(nonWindPowerplants, request.Fuels, request.Load);

            //Both combination of powerplants can meet the load, we compare their costs and use the one with the lowest cost.
            if (resultToAddToWindPowerplants is not null && resultsWithoutWindPowerplants is not null)
            {
                var costWithWindPowerplants = ComputeCost(resultToAddToWindPowerplants, nonWindPowerplants, request.Fuels, includeCo2Costs);
                var costWithoutWindPowerplants = ComputeCost(resultsWithoutWindPowerplants, nonWindPowerplants, request.Fuels, includeCo2Costs);

                if (costWithWindPowerplants > costWithoutWindPowerplants) results = resultsWithoutWindPowerplants;
                else
                {
                    results = resultToAddToWindPowerplants;
                    useWind = true;
                }
            }
            //only the combination with wind powerplants can meet the load, we use it.
            else if (resultToAddToWindPowerplants is not null)
            {
                results = resultToAddToWindPowerplants;
                useWind = true;
            }
            //only the combination without wind powerplants can meet the load, we use it.
            else if (resultsWithoutWindPowerplants is not null) results = resultsWithoutWindPowerplants;
            else throw new InvalidOperationException("No combination of powerplants can meet the required load.");

            //Step 4 : Order the results to match the order of the powerplants in the merit-order list.
            foreach (var powerplant in orderedPowerplants)
            {
                if (powerplant.Type == PowerplantType.Windturbine)
                {
                    var windProduction = useWind ? powerplant.GetActualPMax(request.Fuels) : 0m;
                    orderedResults.Add(new Result(powerplant.Name, windProduction));
                }
                else
                {
                    var existingResult = results.FirstOrDefault(result => result.Name == powerplant.Name);
                    orderedResults.Add(existingResult ?? new Result(powerplant.Name, 0m));
                }
            }

            return new(orderedResults);
        }

        /// <summary>
        /// Gets a list of usable powerplants from a given list that can meet the required load.
        /// </summary>
        /// <returns>A list of powerplants that succeeds meeting the required load. Null if no combination of powerplants can meet the required load.</returns>
        private static List<Result>? GetUsablePowerplantsFromList(List<Powerplant> orderedPowerplants, Fuel fuels, decimal currentLoad)
        {
            var isScanOver = false;
            var powerplantIndexesToTake = new List<int>();
            var selectedPowerplants = new List<Powerplant>();
            var results = new List<Result>();

            //Step 1 : Scan the ordered powerplants to find a combination of powerplants that can meet the required load, while respecting the Pmin and Pmax constraints.
            for (int index = 0; !isScanOver && index < orderedPowerplants.Count; index++)
            {
                var currentPowerplant = orderedPowerplants.ElementAt(index);

                if (currentPowerplant.GetActualPMax(fuels) > 0)
                {
                    //We check beforehand than if we can add the current powerplant with its PMin to the subset, it won't exceed the current load.
                    var currentSubsetTotalPMin = powerplantIndexesToTake
                        .Sum(currentIndexesRegistered => orderedPowerplants.ElementAt(currentIndexesRegistered).Pmin);

                    if (currentPowerplant.Pmin + currentSubsetTotalPMin > currentLoad)
                        continue;

                    powerplantIndexesToTake.Add(index);

                    var subset = new List<Powerplant>();

                    foreach (var subsetIndex in powerplantIndexesToTake)
                    {
                        subset.Add(orderedPowerplants.ElementAt(subsetIndex));
                    }

                    var totalPmin = subset.Sum(powerplant => powerplant.Pmin);
                    var totalPmax = subset.Sum(powerplant => powerplant.GetActualPMax(fuels));

                    if (currentLoad >= totalPmin && currentLoad <= totalPmax)
                    {
                        selectedPowerplants = subset;
                        isScanOver = true;
                    }
                }
            }

            if (selectedPowerplants.Count == 0) return null;

            //Step 2 : Fill the result list with the selected powerplants, starting with their Pmin values. This ensures that we respect the Pmin constraints of the selected powerplants.
            results.AddRange(selectedPowerplants.Select(powerplant => new Result(powerplant.Name, powerplant.Pmin)));
            var totalOfActualPMin = results.Sum(result => result.P);
            currentLoad -= totalOfActualPMin;

            //Step 3 : Distribute the remaining load among the selected powerplants, starting with the most efficient ones. This ensures that we use the most efficient powerplants first, while respecting their Pmax constraints.
            foreach (var index in powerplantIndexesToTake)
            {
                if (currentLoad > 0)
                {
                    var currentPowerplant = orderedPowerplants.ElementAt(index);
                    var actualPMax = currentPowerplant.GetActualPMax(fuels);
                    var remainingPAvailableForCurrentPowerplant = actualPMax - currentPowerplant.Pmin;

                    if (remainingPAvailableForCurrentPowerplant <= currentLoad)
                    {
                        var resultIndex = results.FindIndex(result => result.Name == currentPowerplant.Name);
                        results[resultIndex] = results[resultIndex] with { P = actualPMax };

                        currentLoad -= remainingPAvailableForCurrentPowerplant;
                    }
                    else
                    {
                        var resultIndex = results.FindIndex(result => result.Name == currentPowerplant.Name);
                        results[resultIndex] = results[resultIndex] with { P = results[resultIndex].P + currentLoad };

                        currentLoad = 0;
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Computes the total cost of the selected powerplants for a given dispatch based on their production and fuel costs.
        /// </summary>
        /// <returns>The total cost for the selected powerplants for a given dispatch contained in the results list</returns>
        private static decimal ComputeCost(List<Result> results, List<Powerplant> powerplants, Fuel fuels, bool includeCo2Costs = false)
        {
            var totalCost = 0m;

            foreach (var result in results)
            {
                var powerplant = powerplants.First(powerplant => powerplant.Name == result.Name);
                totalCost += result.P * powerplant.GetCostEfficiency(fuels, includeCo2Costs);
            }

            return totalCost;
        }
    }
}
