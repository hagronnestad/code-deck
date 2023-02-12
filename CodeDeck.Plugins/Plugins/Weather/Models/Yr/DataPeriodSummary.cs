using System.Text.Json.Serialization;

namespace CodeDeck.Plugins.Plugins.Weather.Models.Yr
{
    public class DataPeriodSummary
    {
        [JsonPropertyName("symbol_code")]
        public string? SymbolCode { get; set; }
    }
}