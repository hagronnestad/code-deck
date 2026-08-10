using CodeDeck.PluginAbstractions;
using Microsoft.Extensions.Logging;
using OpenMacroBoard.SDK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace CodeDeck
{
    public class KeyWrapper
    {
        private readonly ILogger _logger;

        public Profile Profile { get; set; }
        public Page Page { get; set; }
        public Key Key { get; set; }

        public Plugin? Plugin { get; set; }
        public Tile? Tile { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; } = new();
        private readonly CancellationTokenSource? _pluginLinkedCancellationTokenSource;
        private CancellationToken CancellationToken =>
            _pluginLinkedCancellationTokenSource?.Token ?? CancellationTokenSource.Token;

        public Image? Image { get; set; }

        /// <summary>
        /// Cached composed key bitmap image
        /// </summary>
        public KeyBitmap? CachedComposedKeyBitmapImage { get; set; }
        internal object RenderLock { get; } = new();

        private Image? _scaledImageSource;
        private Image? _scaledImage;
        private int _scaledImageSize = -1;

        /// <summary>
        /// Returns <paramref name="source"/> scaled to a square of
        /// <paramref name="size"/>, reusing the previous result while the source
        /// image and size are unchanged. Scaling is the most expensive part of
        /// composing an image tile, and it would otherwise run on every redraw.
        /// </summary>
        public Image GetScaledImage(Image source, int size)
        {
            if (source.Width == size && source.Height == size) return source;

            if (!ReferenceEquals(_scaledImageSource, source) || _scaledImageSize != size || _scaledImage is null)
            {
                _scaledImage?.Dispose();
                _scaledImage = source.Clone(x => x.Resize(size, size));
                _scaledImageSource = source;
                _scaledImageSize = size;
            }

            return _scaledImage;
        }

        /// <summary>
        /// Indicates if the wrapped key is currently being shown
        /// </summary>
        public bool IsShowing { get; set; } = false;

        public event EventHandler<KeyWrapper>? Updated;

        public KeyWrapper(ILogger logger, Profile profile, Page page, Key key, Plugin? plugin)
        {
            _logger = logger;

            Profile = profile;
            Page = page;
            Key = key;
            Plugin = plugin;
            _pluginLinkedCancellationTokenSource = plugin is null
                ? null
                : System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                    CancellationTokenSource.Token,
                    plugin.Context.CancellationToken);
        }

        public async Task InstantiateTileObjectAsync()
        {
            if (Plugin is null || Key.Tile is null) return;

            try
            {
                Tile = Plugin.CreateTileInstance(Key.Tile, Key.Settings);
                if (Tile is null) return;

                Tile.NotifyChange = NotifyChange_Action;

                await Tile.InitializeAsync(Plugin.Context, CancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception during 'Tile.Init' in plugin: {Plugin.Name}; tile: {Tile?.GetType().Name ?? "null"}. Message: '{e.Message}'");
            }
        }

        public void NotifyChange_Action()
        {
            Updated?.Invoke(this, this);
        }

        public async void HandleKeyPressDown()
        {
            if (Tile == null) return;
            if (Key.DisableTilePress is not null && Key.DisableTilePress.Value) return;

            try
            {
                await Tile.OnTilePressDown(CancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogDebug($"<{nameof(Tile.OnTilePressDown)}>: {e.Message}");
            }
        }

        public async void HandleKeyPressUp()
        {
            if (Tile == null) return;
            if (Key.DisableTilePress is not null && Key.DisableTilePress.Value) return;

            try
            {
                await Tile.OnTilePressUp(CancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogDebug($"<{nameof(Tile.OnTilePressUp)}>: {e.Message}");
            }
        }

        public void Cancel()
        {
            CancellationTokenSource.Cancel();
        }

        public async Task DeinitializeAsync()
        {
            if (Tile is null) return;

            try
            {
                await Tile.DeInit();
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception during 'Tile.DeInit' in plugin: {Plugin?.Name ?? "none"}; tile: {Tile.GetType().Name}. Message: '{e.Message}'");
            }
        }

        public void Dispose()
        {
            _pluginLinkedCancellationTokenSource?.Dispose();
            CancellationTokenSource.Dispose();

            lock (RenderLock)
            {
                _scaledImage?.Dispose();
                _scaledImage = null;
                _scaledImageSource = null;

                Image?.Dispose();
                Image = null;
            }
        }
    }
}
