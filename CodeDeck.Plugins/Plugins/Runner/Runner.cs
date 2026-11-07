using SixLabors.ImageSharp;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace CodeDeck.Plugins.Plugins.Runner
{
    public class Runner : CodeDeckPlugin
    {
        public class RunTile : Tile
        {
            [Setting] public string? Program { get; set; }
            [Setting] public string? Arguments { get; set; }
            [Setting] public bool? UseShellExecute { get; set; }

            public override Task Init(CancellationToken cancellationToken)
            {
                if (OperatingSystem.IsWindows() && Program is not null)
                {
                    SetOwnedImage(GetAssociatedIcon(Program));
                }

                return base.Init(cancellationToken);
            }

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

                return base.OnTilePressDown(cancellationToken);
            }

            [SupportedOSPlatform("windows")]
            private Image? GetAssociatedIcon(string fileName)
            {
                var fullFileName = GetFileNameUsingEnvironmentPath(fileName);
                if (!File.Exists(fullFileName)) return null;

                using var icon = System.Drawing.Icon.ExtractAssociatedIcon(fullFileName);
                if (icon != null)
                {
                    using var ms = new MemoryStream();
                    using var bitmap = icon.ToBitmap();
                    bitmap.Save(ms, ImageFormat.Png);
                    ms.Seek(0, SeekOrigin.Begin);
                    return Image.Load(ms);
                }

                return null;
            }

            private string? GetFileNameUsingEnvironmentPath(string fileName)
            {
                if (File.Exists(fileName)) return fileName;

                var result = (Environment.GetEnvironmentVariable("PATH") ?? "")
                    .Split(';')
                    .Select(x => Path.Combine(x, fileName))
                    .Where(x => File.Exists(x))
                    .FirstOrDefault();

                return result;
            }
        }

        public class OpenWebsiteTile : Tile
        {
            [Setting] public string? Url { get; set; }

            public override async Task Init(CancellationToken cancellationToken)
            {
                if (Url == null) return;

                var uri = new Uri(Url);
                var client = PluginContext.GetOrCreateSharedState("Runner.HttpClient", () => new HttpClient());
                var favicon = await client.GetByteArrayAsync(
                    $"https://www.google.com/s2/favicons?domain={uri.Host}&sz=64", cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                SetOwnedImage(Image.Load(favicon));
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
