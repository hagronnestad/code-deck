using CodeDeck.PluginAbstractions;
using SixLabors.ImageSharp;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.Runner
{
    public class Runner : CodeDeckPlugin
    {
        private static HttpClient _httpClient = new();

        public class RunTile : Tile
        {
            public override Task OnTilePressDown()
            {
                var program = Settings?["program"];
                if (program == null) return Task.CompletedTask; ;

                Process.Start(program);

                return Task.CompletedTask;
            }
        }

        public class ShellRunTile : Tile
        {
            public override async Task OnTilePressDown()
            {
                var program = Settings?["program"];
                if (program == null) return;

                Process.Start(new ProcessStartInfo()
                {
                    FileName = program,
                    UseShellExecute = true,
                });

                await Task.CompletedTask;
            }
        }

        public class OpenWebsiteTile : Tile
        {
            public override async Task Init()
            {
                var url = Settings?["url"];
                if (url == null) return;

                var uri = new Uri(url);
                var favicon = await _httpClient.GetByteArrayAsync($"http://www.google.com/s2/favicons?domain={uri.Host}&sz=64");
                Image = Image.Load(favicon);
            }

            public override async Task OnTilePressDown()
            {
                var url = Settings?["url"];
                if (url == null) return;

                Process.Start(new ProcessStartInfo()
                {
                    FileName = url,
                    UseShellExecute = true,
                });

                await Task.CompletedTask;
            }
        }
    }
}
