using power_plant_coding_challenge_domain.Enums;

namespace power_plant_coding_challenge_domain.Models;

public class Powerplant
{
    public required string Name { get; set; }
    public PowerplantType Type { get; set; }
    public decimal Efficiency { get; set; }
    public int Pmin { get; set; }
    public int Pmax { get; set; }

    /// <summary>
    /// returns the actual Pmax of a powerplant based on its type and the fuels available. For wind turbines, it takes into account the wind percentage to calculate the actual Pmax.
    /// </summary>
    /// <returns>The actual Pmax for the given powerplant and fuels.</returns>
    /// /// <exception cref="ArgumentOutOfRangeException">Thrown if the PowerplantType is unknown</exception>
    public decimal GetActualPMax(Fuel fuels)
    {
        switch (Type)
        {
            case PowerplantType.Windturbine:
                var windEfficiencyPercentage = fuels.Wind / 100.0m;
                return Pmax * windEfficiencyPercentage;
            case PowerplantType.Gasfired:
                return Pmax;
            case PowerplantType.Turbojet:
                return Pmax;
            default:
                throw new ArgumentOutOfRangeException($"Unknown powerplant type: {Type}");
        }
    }

    /// <summary>
    /// returns the cost efficiency of a powerplant based on its type, dividing the fuel price by the powerplant efficiency.
    /// </summary>
    /// <returns>An efficiency measure under a double. the more the value tends to 0, the more efficient the powerplant is.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the PowerplantType is unknown</exception>
    #pragma warning disable IDE0066
    public decimal GetCostEfficiency(Fuel fuels)
    {
        switch (Type)
        {
            case PowerplantType.Windturbine:
                return 0;
            case PowerplantType.Gasfired:
                return fuels.Gas / Efficiency;
            case PowerplantType.Turbojet:
                return fuels.Kerosine / Efficiency;
            default:
                throw new ArgumentOutOfRangeException($"Unknown powerplant type: {Type}");
        }
    }
}
