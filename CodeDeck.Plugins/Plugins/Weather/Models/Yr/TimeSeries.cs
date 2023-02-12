using System;
using System.Text.Json.Serialization;

namespace CodeDeck.Plugins.Plugins.Weather.Models.Yr
{
    public class TimeSeries
    {
        [JsonPropertyName("time")]
        public DateTimeOffset Time { get; set; }

        [JsonPropertyName("data")]
        public TimeSeriesData? Data { get; set; }
    }
}
