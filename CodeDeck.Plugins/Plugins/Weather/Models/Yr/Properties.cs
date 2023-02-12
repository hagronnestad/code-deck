using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CodeDeck.Plugins.Plugins.Weather.Models.Yr
{
    public class Properties
    {
        [JsonPropertyName("timeseries")]
        public List<TimeSeries>? Timeseries { get; set; }
    }
}
