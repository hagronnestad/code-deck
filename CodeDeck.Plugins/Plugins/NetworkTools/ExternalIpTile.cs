using CodeDeck.PluginAbstractions;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.NetworkTools;

public partial class NetworkTools : CodeDeckPlugin
{
    public class ExternalIpTile : Tile
    {
        [Setting] public string ApiUrl { get; set; } = "https://api.ipify.org";
        [Setting] public string Format { get; set; } = "Ext. IP:\n{0}";
        [Setting] public int Interval { get; set; } = 60000;
        [Setting] public bool PadOctets { get; set; } = false;
        [Setting] public bool ShowIpOnTwoLines { get; set; } = true;

        private readonly HttpClient _client = new();


        public override async Task Init(CancellationToken cancellationToken)
        {
            _ = Task.Run(() => BackgroundTask(cancellationToken), cancellationToken);

            await Task.CompletedTask;

        }

        public override async Task OnTilePressUp(CancellationToken cancellationToken)
        {
            await GetAsync(cancellationToken);
        }

        private async Task GetAsync(CancellationToken cancellationToken)
        {
            try
            {
                ShowIndicator = true;
                var result = await _client.GetStringAsync(ApiUrl, cancellationToken);

                Text = string.Format(Format, FormatIp(result, ShowIpOnTwoLines, PadOctets));
            }
            catch (Exception) { }
            finally
            {
                ShowIndicator = false;
            }
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

        private static string FormatIp(string ip, bool multiline, bool pad)
        {
            try
            {
                var parts = ip.Split('.');
                if (parts.Length != 4) return ip;

                var ints = parts.Select(x => int.Parse(x)).ToList();

                if (multiline)
                {
                    if (pad)
                    {
                        return $"{ints[0]:D3}.{ints[1]:D3}\n{ints[2]:D3}.{ints[3]:D3}";
                    }
                    else
                    {
                        return $"{ints[0]}.{ints[1]}\n{ints[2]}.{ints[3]}";
                    }
                }
                else
                {
                    if (pad)
                    {
                        return $"{ints[0]:D3}.{ints[1]:D3}.{ints[2]:D3}.{ints[3]:D3}";
                    }
                    else
                    {
                        return $"{ints[0]}.{ints[1]}.{ints[2]}.{ints[3]}";
                    }
                }
            }
            catch (Exception)
            {
                return ip;
            }
        }
    }
}
