using power_plant_coding_challenge_core.features;
using power_plant_coding_challenge_domain.enums;
using power_plant_coding_challenge_domain.models;

namespace power_plant_coding_challenge_tests;

public class CalculateProductionPlanShould
{
    private readonly CalculateProductionPlan.Handler _sut = new();

    //Powerplants coming from payload1/2/3.json
    private static List<Powerplant> DefaultPowerplants =>
    [
        new() { Name = "gasfiredbig1", Type = PowerplantType.gasfired, Efficiency = 0.53m, Pmin = 100, Pmax = 460 },
        new() { Name = "gasfiredbig2", Type = PowerplantType.gasfired, Efficiency = 0.53m, Pmin = 100, Pmax = 460 },
        new() { Name = "gasfiredsomewhatsmaller", Type = PowerplantType.gasfired, Efficiency = 0.37m, Pmin = 40,  Pmax = 210 },
        new() { Name = "tj1", Type = PowerplantType.turbojet, Efficiency = 0.3m,  Pmin = 0, Pmax = 16  },
        new() { Name = "windpark1", Type = PowerplantType.windturbine, Efficiency = 1m, Pmin = 0, Pmax = 150 },
        new() { Name = "windpark2", Type = PowerplantType.windturbine, Efficiency = 1m, Pmin = 0, Pmax = 36  },
    ];

    // Payload 1 : load=480, wind=60%
    [Fact]
    public async Task ReturnCorrectPlan_ForPayload1()
    {
        var command = new CalculateProductionPlan.Command(
            load: 480,
            fuels: new Fuel { Gas = 13.4m, Kerosine = 50.8m, Co2 = 20m, Wind = 60m },
            Powerplants: DefaultPowerplants
        );

        var response = await _sut.Handle(command, CancellationToken.None);
        var results = response.Results.ToDictionary(r => r.Name, r => r.P);

        Assert.Equal(90.0m, results["windpark1"]);
        Assert.Equal(21.6m, results["windpark2"]);
        Assert.Equal(368.4m, results["gasfiredbig1"]);
        Assert.Equal(0m, results["gasfiredbig2"]);
        Assert.Equal(0m, results["gasfiredsomewhatsmaller"]);
        Assert.Equal(0m, results["tj1"]);
        Assert.Equal(480m, results.Values.Sum());
    }

    // Payload 2 : load=480, wind=0%
    [Fact]
    public async Task ReturnCorrectPlan_ForPayload2()
    {
        var command = new CalculateProductionPlan.Command(
            load: 480,
            fuels: new Fuel { Gas = 13.4m, Kerosine = 50.8m, Co2 = 20m, Wind = 0m },
            Powerplants: DefaultPowerplants
        );

        var response = await _sut.Handle(command, CancellationToken.None);
        var results = response.Results.ToDictionary(r => r.Name, r => r.P);

        Assert.Equal(0m, results["windpark1"]);
        Assert.Equal(0m, results["windpark2"]);
        Assert.Equal(380m, results["gasfiredbig1"]);
        Assert.Equal(100m, results["gasfiredbig2"]);
        Assert.Equal(0m, results["gasfiredsomewhatsmaller"]);
        Assert.Equal(0m, results["tj1"]);
        Assert.Equal(480m, results.Values.Sum());
    }

    // Payload 3 : load=910, wind=60% — réponse de référence fournie dans response3.json
    [Fact]
    public async Task ReturnCorrectPlan_ForPayload3()
    {
        var command = new CalculateProductionPlan.Command(
            load: 910,
            fuels: new Fuel { Gas = 13.4m, Kerosine = 50.8m, Co2 = 20m, Wind = 60m },
            Powerplants: DefaultPowerplants
        );

        var response = await _sut.Handle(command, CancellationToken.None);
        var results = response.Results.ToDictionary(r => r.Name, r => r.P);

        Assert.Equal(90.0m, results["windpark1"]);
        Assert.Equal(21.6m, results["windpark2"]);
        Assert.Equal(460.0m, results["gasfiredbig1"]);
        Assert.Equal(338.4m, results["gasfiredbig2"]);
        Assert.Equal(0m, results["gasfiredsomewhatsmaller"]);
        Assert.Equal(0m, results["tj1"]);
        Assert.Equal(910m, results.Values.Sum());
    }

    // La somme des productions doit toujours égaler le load
    [Theory]
    [InlineData(480, 60)]
    [InlineData(480, 0)]
    [InlineData(910, 60)]
    public async Task ProductionSumAlwaysEqualsLoad(int load, decimal windPercent)
    {
        var command = new CalculateProductionPlan.Command(
            load: load,
            fuels: new Fuel { Gas = 13.4m, Kerosine = 50.8m, Co2 = 20m, Wind = windPercent },
            Powerplants: DefaultPowerplants
        );

        var response = await _sut.Handle(command, CancellationToken.None);

        Assert.Equal(load, response.Results.Sum(r => r.P));
    }

    // Load = 0 : toutes les centrales doivent produire 0
    [Fact]
    public async Task AllPlantsProduceZero_WhenLoadIsZero()
    {
        var command = new CalculateProductionPlan.Command(
            load: 0,
            fuels: new Fuel { Gas = 13.4m, Kerosine = 50.8m, Co2 = 20m, Wind = 60m },
            Powerplants: DefaultPowerplants
        );

        var response = await _sut.Handle(command, CancellationToken.None);

        Assert.All(response.Results, r => Assert.Equal(0m, r.P));
    }

    // Wind = 0% : les éoliennes ne produisent rien
    [Fact]
    public async Task WindturbineProducesZero_WhenWindIsZero()
    {
        var command = new CalculateProductionPlan.Command(
            load: 480,
            fuels: new Fuel { Gas = 13.4m, Kerosine = 50.8m, Co2 = 20m, Wind = 0m },
            Powerplants: DefaultPowerplants
        );

        var response = await _sut.Handle(command, CancellationToken.None);
        var results = response.Results.ToDictionary(r => r.Name, r => r.P);

        Assert.Equal(0m, results["windpark1"]);
        Assert.Equal(0m, results["windpark2"]);
    }

    // Chaque centrale ne dépasse pas son Pmax
    [Fact]
    public async Task NoPowerplantExceedsPmax()
    {
        var command = new CalculateProductionPlan.Command(
            load: 910,
            fuels: new Fuel { Gas = 13.4m, Kerosine = 50.8m, Co2 = 20m, Wind = 60m },
            Powerplants: DefaultPowerplants
        );

        var response = await _sut.Handle(command, CancellationToken.None);

        foreach (var result in response.Results)
        {
            var plant = DefaultPowerplants.Single(p => p.Name == result.Name);
            decimal effectivePmax = plant.Type == PowerplantType.windturbine
                ? plant.Pmax * (60m / 100m)
                : plant.Pmax;

            Assert.True(result.P <= effectivePmax,
                $"{result.Name} produit {result.P} MW mais son Pmax est {effectivePmax} MW");
        }
    }

    // Une centrale allumée doit respecter son Pmin
    [Fact]
    public async Task NoPowerplantBelowPmin_WhenActive()
    {
        var command = new CalculateProductionPlan.Command(
            load: 910,
            fuels: new Fuel { Gas = 13.4m, Kerosine = 50.8m, Co2 = 20m, Wind = 60m },
            Powerplants: DefaultPowerplants
        );

        var response = await _sut.Handle(command, CancellationToken.None);

        foreach (var result in response.Results)
        {
            if (result.P == 0m) continue; // centrale éteinte, Pmin ne s'applique pas

            var plant = DefaultPowerplants.Single(p => p.Name == result.Name);
            Assert.True(result.P >= plant.Pmin,
                $"{result.Name} produit {result.P} MW mais son Pmin est {plant.Pmin} MW");
        }
    }
}
