using CodeDeck.Plugins.Plugins.Weather.Models.Yr;
using SixLabors.ImageSharp;
using System.Globalization;
using System.Net.Http.Json;

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

            private readonly SemaphoreSlim _updateLock = new(1, 1);
            private bool _deinitialized;
            private string _apiUrl = "";

            public override Task Init(CancellationToken cancellationToken)
            {
                if (!double.IsFinite(Lat) || Lat is < -90 or > 90 ||
                    !double.IsFinite(Lon) || Lon is < -180 or > 180)
                {
                    throw new InvalidOperationException("Weather coordinates must be valid latitude and longitude values.");
                }
                if (Period is not (null or "Next1Hours" or "Next6Hours" or "Next12Hours"))
                {
                    throw new InvalidOperationException("Weather Period must be Next1Hours, Next6Hours, or Next12Hours.");
                }

                _apiUrl = $"https://api.met.no/weatherapi/locationforecast/2.0/compact?lat={Lat.ToString("0.####", CultureInfo.InvariantCulture)}&lon={Lon.ToString("0.####", CultureInfo.InvariantCulture)}";
                RunEvery("Weather update", TimeSpan.FromMilliseconds(Interval), GetAsync);
                return Task.CompletedTask;
            }

            public override Task OnTilePressUp(CancellationToken cancellationToken) => GetAsync(cancellationToken);

            public override async Task DeInit()
            {
                await _updateLock.WaitAsync();
                try
                {
                    _deinitialized = true;
                }
                finally
                {
                    _updateLock.Release();
                }
            }

            private async Task GetAsync(CancellationToken cancellationToken)
            {
                await _updateLock.WaitAsync(cancellationToken);
                try
                {
                    ObjectDisposedException.ThrowIf(_deinitialized, this);
                    ShowIndicator = true;
                    var client = PluginContext.GetOrCreateSharedState("Weather.HttpClient", () =>
                    {
                        var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                            "CodeDeck/1.0 (https://github.com/hagronnestad/code-deck)");
                        return httpClient;
                    });
                    var data = await client.GetFromJsonAsync<LocationForecast>(_apiUrl, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    var timeSeriesData = data?.Properties?.Timeseries?.FirstOrDefault()?.Data
                        ?? throw new InvalidOperationException("Weather API returned no forecast.");
                    var symbolCode = Period switch
                    {
                        null or "Next1Hours" => timeSeriesData.Next1Hours?.Summary?.SymbolCode,
                        "Next6Hours" => timeSeriesData.Next6Hours?.Summary?.SymbolCode,
                        "Next12Hours" => timeSeriesData.Next12Hours?.Summary?.SymbolCode,
                        _ => throw new InvalidOperationException("Unknown weather period.")
                    };
                    if (string.IsNullOrWhiteSpace(symbolCode))
                    {
                        throw new InvalidOperationException($"Weather API returned no symbol for {Period ?? "Next1Hours"}.");
                    }
                    if (symbolCode.Any(character => !(char.IsAsciiLetterOrDigit(character) || character == '_')))
                    {
                        throw new InvalidOperationException("Weather API returned an invalid symbol code.");
                    }

                    var symbolFile = Path.Combine(PluginContext.PluginPath, "Icons", $"{symbolCode}.png");
                    var image = Image.Load(symbolFile);
                    SetOwnedImage(image);
                    Logger.LogDebug("Weather image updated for {Period}.", Period ?? "Next1Hours");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Weather image update failed for {Period}.", Period ?? "Next1Hours");
                    throw;
                }
                finally
                {
                    ShowIndicator = false;
                    _updateLock.Release();
                }
            }
        }
    }
}
