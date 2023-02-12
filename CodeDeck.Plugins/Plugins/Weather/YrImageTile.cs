using CodeDeck.PluginAbstractions;
using SixLabors.ImageSharp;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Json;
using CodeDeck.Plugins.Plugins.Weather.Models.Yr;
using System.IO;
using System.Globalization;
using System.Linq;

namespace CodeDeck.Plugins.Plugins.Weather
{
    public partial class Weather : CodeDeckPlugin
    {
        public class YrImageTile : Tile
        {
            [Setting] public int Interval { get; set; } = 10 * 60 * 1000;
            [Setting] public double Lat { get; set; }
            [Setting] public double Lon { get; set; }
            [Setting] public string? Period { get; set; }


            private static readonly HttpClient _client = new();
            private const string API_BASE_URL = "https://api.met.no/weatherapi/locationforecast/2.0/";
            private string _apiUrl = "";

            public override async Task Init(CancellationToken cancellationToken)
            {
                _client.DefaultRequestHeaders.Add("User-Agent", "Code Deck");

                var nfi = new NumberFormatInfo
                {
                    NumberDecimalSeparator = ".",
                    NumberDecimalDigits = 4
                };

                _apiUrl = $"{API_BASE_URL}compact?lat={Lat.ToString(nfi)}&lon={Lon.ToString(nfi)}";

                _ = Task.Run(() => BackgroundTask(cancellationToken), cancellationToken);

                await Task.CompletedTask;
            }

            public override async Task OnTilePressUp(CancellationToken cancellationToken)
            {
                await GetAsync(cancellationToken);
            }

            private async Task<LocationForecast?> GetAsync(CancellationToken cancellationToken)
            {
                try
                {
                    ShowIndicator = true;
                    var data = await _client.GetFromJsonAsync<LocationForecast>(_apiUrl, cancellationToken);

                    var timeSeries = data?.Properties?.Timeseries;
                    if (timeSeries is null || !timeSeries.Any()) return null;

                    var timeSeriesData = timeSeries.First().Data;

                    string? symbolCode = null;

                    switch (Period)
                    {
                        case null:
                        case "Next1Hours":
                            symbolCode = timeSeriesData?.Next1Hours?.Summary?.SymbolCode;
                            break;
                        case "Next6Hours":
                            symbolCode = timeSeriesData?.Next6Hours?.Summary?.SymbolCode;
                            break;
                        case "Next12Hours":
                            symbolCode = timeSeriesData?.Next12Hours?.Summary?.SymbolCode;
                            break;

                        default:
                            break;
                    }

                    if (symbolCode is null) return null;

                    var symbolFile = Path.GetFullPath($"Plugins/Weather/Icons/{symbolCode}.png");
                    if (!File.Exists(symbolFile)) return null;

                    Image = Image.Load(symbolFile);

                    return data;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    ShowIndicator = false;
                }

                return null;
            }

            private async Task BackgroundTask(CancellationToken cancellationToken)
            {
                for (; ; )
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    GetAsync(cancellationToken).Wait(cancellationToken);
                    await Task.Delay(Interval, cancellationToken);
                }
            }
        }
    }
}
