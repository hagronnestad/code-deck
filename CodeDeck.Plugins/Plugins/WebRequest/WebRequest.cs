using CodeDeck.PluginAbstractions;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
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

            public override async Task OnTilePressUp(CancellationToken cancellationToken)
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

        public class ImageTile : Tile
        {
            [Setting] public string? Url { get; set; }
            [Setting] public int Interval { get; set; } = 60000;
            [Setting] public int Size { get; set; } = 72;

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
                    var data = await _client.GetByteArrayAsync(Url, cancellationToken);
                    var image = SixLabors.ImageSharp.Image.Load(data);

                    if (image != null)
                    {
                        image.Mutate(i => i.Resize(new ResizeOptions()
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(Size, Size)
                        }));

                        var i = new Image<Rgba32>(Size, Size);
                        i.Mutate(x => x.DrawImage(image, new Point((Size / 2) - image.Width / 2, (Size / 2) - image.Height / 2), 1f));

                        Image = i;
                    }
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
        }
    }
}
