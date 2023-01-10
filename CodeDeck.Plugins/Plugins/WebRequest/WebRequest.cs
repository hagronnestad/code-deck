using CodeDeck.PluginAbstractions;
using System.Net.Http;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.WebRequest
{
    public class WebRequest : CodeDeckPlugin
    {
        private static readonly HttpClient _client = new();

        public class PlainTextTile : Tile
        {
            private string? _url;
            private string? _format;
            private int? _interval;

            public override async Task Init()
            {
                if (Settings == null) return;

                if (Settings.TryGetValue("url", out var url))
                {
                    _url = url;
                }

                if (Settings.TryGetValue("format", out var format))
                {
                    _format = format;
                }

                if (Settings.TryGetValue("interval", out var interval))
                {
                    if (int.TryParse(interval, out var i)) _interval = i;
                }

                _ = Task.Run(BackgroundTask);

                await Task.CompletedTask;
            }

            public override async Task OnTilePressDown()
            {
                await GetAsync();
            }

            private async Task GetAsync()
            {
                ShowIndicator = true;
                Text = string.Format(_format ?? "{0}", await _client.GetStringAsync(_url));
                ShowIndicator = false;
            }

            private async Task BackgroundTask()
            {
                while (true)
                {
                    await GetAsync();
                    await Task.Delay(_interval ?? 60000);
                }
            }
        }
    }
}
