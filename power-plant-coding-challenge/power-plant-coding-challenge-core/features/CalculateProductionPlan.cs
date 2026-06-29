using FluentValidation;
using MediatR;
using power_plant_coding_challenge_domain.enums;
using power_plant_coding_challenge_domain.models;

namespace power_plant_coding_challenge_core.features;

public static class CalculateProductionPlan
{
    public record Command(
        int load,
        Fuel fuels,
        List<Powerplant> Powerplants) : IRequest<Response>;
    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(command => command.load)
                .GreaterThanOrEqualTo(0).WithMessage("Load cannot be negative.");

            RuleFor(command => command.fuels)
                .NotNull().WithMessage("Fuels are required.");

            When(command => command.fuels is not null, () =>
            {
                RuleFor(command => command.fuels.Gas)
                    .GreaterThanOrEqualTo(0).WithMessage("Gas price cannot be negative.");

                RuleFor(command => command.fuels.Kerosine)
                    .GreaterThanOrEqualTo(0).WithMessage("Kerosine price cannot be negative.");

                RuleFor(command => command.fuels.Wind)
                    .InclusiveBetween(0, 100).WithMessage("Wind percentage must be between 0 and 100.");
            });

            RuleFor(command => command.Powerplants)
                .NotNull().NotEmpty().WithMessage("At least one powerplant is required.");

            RuleForEach(command => command.Powerplants).ChildRules(powerplant =>
            {
                powerplant.RuleFor(powerplant => powerplant.Name)
                    .NotEmpty().WithMessage("Powerplant name is required.");

                powerplant.RuleFor(powerplant => powerplant.Efficiency)
                    .InclusiveBetween(0, 1).WithMessage(powerplant => $"Efficiency of '{powerplant.Name}' must be between 0 and 1.");

                powerplant.RuleFor(powerplant => powerplant.Pmin)
                    .GreaterThanOrEqualTo(0).WithMessage(powerplant => $"Pmin of '{powerplant.Name}' cannot be negative.");

                powerplant.RuleFor(powerplant => powerplant.Pmax)
                    .GreaterThan(0).WithMessage(powerplant => $"Pmax of '{powerplant.Name}' must be greater than 0.")
                    .GreaterThanOrEqualTo(powerplant => powerplant.Pmin).WithMessage(powerplant => $"Pmax of '{powerplant.Name}' must be superior or equal to Pmin.");
            });
        }
    }
    public record Response(List<Result> Results);
    public class Result
    {
        public Result(string name, decimal p)
        {
            Name = name;
            P = p;
        }

        public string Name { get; }
        public decimal P { get; }
    }
    public class Handler : IRequestHandler<Command, Response>
    {
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var orderedPowerplants = request.Powerplants
                .OrderBy(powerplant => GetCostEfficiency(powerplant, request.fuels))
                .ThenByDescending(powerplant => powerplant.Pmax)
                .ToList();

            decimal currentLoad = request.load;

            var results = new List<Result>();

            for (int index = 0; index < orderedPowerplants.Count; index++)
            {
                var currentPowerplant = orderedPowerplants[index];

                if (currentLoad == 0) results.Add(new Result(currentPowerplant.Name, 0));

                if (currentLoad > 0)
                {
                    var nextPowerplant = index + 1 < orderedPowerplants.Count ? orderedPowerplants[index + 1] : null;

                    decimal pMax = GetPMaxForActualPowerplant(currentPowerplant, request.fuels);

                    if (currentLoad > pMax)
                    {
                        var loadRemaining = currentLoad - pMax;

                        if (nextPowerplant is not null && loadRemaining < nextPowerplant.Pmin)
                        {
                            var loadToFill = currentLoad - nextPowerplant.Pmin;
                            results.Add(new Result(currentPowerplant.Name, loadToFill));
                            currentLoad -= loadToFill;
                        }
                        else
                        {
                            results.Add(new Result(currentPowerplant.Name, pMax));
                            currentLoad -= pMax;
                        }
                    }
                    else if (pMax > 0)
                    {
                        results.Add(new Result(currentPowerplant.Name, currentLoad));
                        currentLoad = 0;
                    }
                }
            }

            return new Response(results);
        }

        /// <summary>
        /// returns the cost efficiency of a powerplant based on its type and the fuel prices. Made to be used in the OrderBy clause to sort the powerplants by cost efficiency.
        /// </summary>
        /// <returns>An efficiency measure under a double. the more the value tends to 0, the more efficient the powerplant is.</returns>
        /// <exception cref="Exception"></exception>
        private static decimal GetCostEfficiency(Powerplant powerplant, Fuel fuels)
        {
            switch (powerplant.Type)
            {
                case PowerplantType.windturbine:
                    return 0;
                case PowerplantType.gasfired:
                    return fuels.Gas / powerplant.Efficiency;
                case PowerplantType.turbojet:
                    return fuels.Kerosine / powerplant.Efficiency;

                default:
                    throw new Exception($"Unknown powerplant type: {powerplant.Type}");
            }
        }

        private decimal GetPMaxForActualPowerplant(Powerplant powerplant, Fuel fuels)
        {
            switch (powerplant.Type)
            {
                case PowerplantType.windturbine:
                    var windEfficiencyPercentage = fuels.Wind / 100.0m;
                    return powerplant.Pmax * windEfficiencyPercentage;
                case PowerplantType.gasfired:
                    return powerplant.Pmax;
                case PowerplantType.turbojet:
                    return powerplant.Pmax;
                default:
                    return 0;
            }
        }
    }
}
