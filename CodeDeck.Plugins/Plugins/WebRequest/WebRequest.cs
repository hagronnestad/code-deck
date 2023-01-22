using CodeDeck.PluginAbstractions;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.WebRequest
{
    public class WebRequest : CodeDeckPlugin
    {
        private static readonly HttpClient _client = new();

        public class PlainTextTile : Tile
        {
            [Setting] public string? Url { get; set; }
            [Setting] public string? Format { get; set; }
            [Setting] public int? Interval { get; set; }

            public override async Task Init(CancellationToken cancellationToken)
            {
                _ = Task.Run(() => BackgroundTask(cancellationToken), cancellationToken);

                await Task.CompletedTask;
            }

            public override async Task OnTilePressDown(CancellationToken cancellationToken)
            {
                await GetAsync(cancellationToken);
            }

            private async Task GetAsync(CancellationToken cancellationToken)
            {
                try
                {
                    ShowIndicator = true;
                    Text = string.Format(Format ?? "{0}", await _client.GetStringAsync(Url, cancellationToken));
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
                        Debug.WriteLine($"{nameof(BackgroundTask)} in {nameof(PlainTextTile)} with {Url} was cancelled!");
                        return;
                    }

                    GetAsync(cancellationToken).Wait(cancellationToken);
                    await Task.Delay(Interval ?? 60000, cancellationToken);
                }
            }
        }
    }
}
