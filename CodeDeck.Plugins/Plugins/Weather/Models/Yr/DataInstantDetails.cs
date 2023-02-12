using System.Text.Json.Serialization;

namespace CodeDeck.Plugins.Plugins.Weather.Models.Yr
{
    public class DataInstantDetails
    {
        [JsonPropertyName("air_pressure_at_sea_level")]
        public double AirPressureAtSeaLevel { get; set; }
        
        [JsonPropertyName("air_temperature")]
        public double AirTemperature { get; set; }
        
        [JsonPropertyName("wind_from_direction")]
        public double WindFromDirection { get; set; }
        
        [JsonPropertyName("wind_speed")]
        public double WindSpeed { get; set; }
    }
}
