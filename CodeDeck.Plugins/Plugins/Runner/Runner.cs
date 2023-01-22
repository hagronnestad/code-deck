using CodeDeck.PluginAbstractions;
using SixLabors.ImageSharp;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.Runner
{
    public class Runner : CodeDeckPlugin
    {
        private static readonly HttpClient _httpClient = new();

        public class RunTile : Tile
        {
            [Setting] public string? Program { get; set; }
            [Setting] public string? Arguments { get; set; }
            [Setting] public bool? UseShellExecute { get; set; }

            public override Task OnTilePressDown(CancellationToken cancellationToken)
            {
                if (Program is not null)
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = Program,
                        Arguments = Arguments ?? "",
                        UseShellExecute = UseShellExecute ?? false
                    });
                }

                return Task.CompletedTask;
            }
        }

        public class OpenWebsiteTile : Tile
        {
            [Setting] public string? Url { get; set; }

            public override async Task Init(CancellationToken cancellationToken)
            {
                if (Url == null) return;

                var uri = new Uri(Url);
                var favicon = await _httpClient.GetByteArrayAsync($"http://www.google.com/s2/favicons?domain={uri.Host}&sz=64");
                Image = Image.Load(favicon);
            }

            public override async Task OnTilePressDown(CancellationToken cancellationToken)
            {
                if (Url == null) return;

                Process.Start(new ProcessStartInfo()
                {
                    FileName = Url,
                    UseShellExecute = true,
                });

                await Task.CompletedTask;
            }
        }
    }
}
