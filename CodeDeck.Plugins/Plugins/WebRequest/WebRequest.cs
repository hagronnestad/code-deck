using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CodeDeck.Plugins.Plugins.WebRequest
{
    public class WebRequest : CodeDeckPlugin
    {
        public abstract class WebRequestTile : Tile
        {
            private readonly SemaphoreSlim _updateLock = new(1, 1);
            private bool _deinitialized;

            [Setting] public string? Url { get; set; }
            protected abstract int IntervalMilliseconds { get; }
            protected abstract string UpdateName { get; }
            protected abstract Task FetchAsync(HttpClient client, Uri url, CancellationToken cancellationToken);

            public override Task Init(CancellationToken cancellationToken)
            {
                RunEvery(UpdateName, TimeSpan.FromMilliseconds(IntervalMilliseconds), GetAsync);
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
                    if (!Uri.TryCreate(Url, UriKind.Absolute, out var url) ||
                        url.Scheme is not ("http" or "https"))
                    {
                        throw new InvalidOperationException("WebRequest requires an absolute HTTP or HTTPS URL.");
                    }

                    var client = PluginContext.GetOrCreateSharedState("WebRequest.HttpClient", () => new HttpClient());
                    await FetchAsync(client, url, cancellationToken);
                    Logger.LogDebug("Web request update completed for {Host}.", url.Host);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Web request update failed.");
                    throw;
                }
                finally
                {
                    ShowIndicator = false;
                    _updateLock.Release();
                }
            }
        }

        public class PlainTextTile : WebRequestTile
        {
            [Setting] public string? Format { get; set; }
            [Setting] public int? Interval { get; set; }

            protected override int IntervalMilliseconds => Interval ?? 60000;
            protected override string UpdateName => "Plain text update";

            protected override async Task FetchAsync(HttpClient client, Uri url, CancellationToken cancellationToken)
            {
                var text = await client.GetStringAsync(url, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                Text = string.Format(Format ?? "{0}", text);
            }
        }

        public class ImageTile : WebRequestTile
        {
            [Setting] public int Interval { get; set; } = 60000;
            [Setting] public int Size { get; set; } = 72;
            [Setting] public bool Crop { get; set; } = false;

            protected override int IntervalMilliseconds => Interval;
            protected override string UpdateName => "Image update";

            protected override async Task FetchAsync(HttpClient client, Uri url, CancellationToken cancellationToken)
            {
                if (Size <= 0) throw new InvalidOperationException("WebRequest image Size must be positive.");
                var data = await client.GetByteArrayAsync(url, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
                try
                {
                    if (Crop)
                    {
                        var cropSize = Math.Min(image.Width, image.Height);
                        image.Mutate(x => x.Crop(new Rectangle(
                            (image.Width - cropSize) / 2, (image.Height - cropSize) / 2, cropSize, cropSize)));
                    }
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Pad,
                        Size = new Size(Size, Size),
                        PadColor = Color.Transparent
                    }));
                    cancellationToken.ThrowIfCancellationRequested();
                    SetOwnedImage(image);
                }
                catch
                {
                    if (!ReferenceEquals(Image, image)) image.Dispose();
                    throw;
                }
            }
        }
    }
}
