using System.Text.Json.Serialization;

namespace power_plant_coding_challenge_domain.enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PowerplantType
{
   
    windturbine,
    gasfired,
    turbojet
}
