using power_plant_coding_challenge_domain.enums;

namespace power_plant_coding_challenge_domain.models;

public class Powerplant
{
    public string Name { get; set; }
    public PowerplantType Type { get; set; }
    public decimal Efficiency { get; set; }
    public int Pmin { get; set; }
    public int Pmax { get; set; }
}
