using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;

namespace CodeDeck.PluginAbstractions
{
    /// <summary>
    /// Represents a Tile, a Tile can react to key presses or just display data
    /// </summary>
    public class Tile
    {
        /// <summary>
        /// Set by the framework, notifies the framework that the tile needs to be updated on the Deck
        /// </summary>
        public Action? NotifyChange { get; set; }


        private string? _text;
        public string? Text
        {
            get { return _text; }
            set { Set(ref _text, value); }
        }

        private Color? _textColor;
        public Color? TextColor
        {
            get => _textColor;
            set { Set(ref _textColor, value); }
        }

        private Color? _bgColor;
        public Color? BackgroundColor
        {
            get => _bgColor;
            set { Set(ref _bgColor, value); }
        }

        private string? _font;
        public string? Font
        {
            get => _font;
            set { Set(ref _font, value); }
        }

        private float? _fontSize;
        public float? FontSize
        {
            get => _fontSize;
            set { Set(ref _fontSize, value); }
        }

        private Image? _image;
        private Image? _ownedImage;
        private bool _isDeinitialized;
        public object RenderLock { get; private set; } = new();
        public Image? Image
        {
            get => _image;
            set { Set(ref _image, value); }
        }

        protected void SetOwnedImage(Image? image)
        {
            lock (RenderLock)
            {
                if (_isDeinitialized)
                {
                    image?.Dispose();
                    throw new ObjectDisposedException(GetType().Name);
                }

                var previous = _ownedImage;
                _ownedImage = image;
                try
                {
                    Image = image;
                }
                finally
                {
                    if (!ReferenceEquals(previous, image)) previous?.Dispose();
                }
            }
        }

        private int? _imagePadding;
        public int? ImagePadding
        {
            get => _imagePadding;
            set { Set(ref _imagePadding, value); }
        }

        private bool? _showIndicator;
        public bool? ShowIndicator
        {
            get => _showIndicator;
            set { Set(ref _showIndicator, value); }
        }

        private Color? _indicatorColor;
        public Color? IndicatorColor
        {
            get => _indicatorColor;
            set { Set(ref _indicatorColor, value); }
        }

        /// <summary>
        /// Assigns a tile property and only notifies the framework when the value
        /// actually changed. Redrawing a key is expensive (bitmap composition plus a
        /// USB transfer), so tiles that poll frequently but rarely change - a clock
        /// showing "HH:mm", a battery level, a weather icon - cost almost nothing.
        /// </summary>
        private void Set<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;

            field = value;
            NotifyChange?.Invoke();
        }

        public Dictionary<string, string>? Settings { get; set; }
        public PluginContext PluginContext { get; private set; } = null!;
        private ILogger? _logger;
        protected ILogger Logger => _logger
            ?? throw new InvalidOperationException("The tile has not been initialized.");
        private CancellationToken _cancellationToken;
        private readonly string _taskOwner;


        public Tile()
        {
            _taskOwner = $"{GetType().Name} ({Guid.NewGuid():N})";
        }

        public async Task InitializeAsync(
            PluginContext pluginContext, CancellationToken cancellationToken, object? renderLock = null)
        {
            RenderLock = renderLock ?? RenderLock;
            PluginContext = pluginContext;
            _logger = pluginContext.CreateLogger(GetType().FullName ?? GetType().Name);
            _cancellationToken = cancellationToken;
            await Init(cancellationToken);
        }

        public async Task DeinitializeAsync()
        {
            try
            {
                await DeInit();
            }
            finally
            {
                lock (RenderLock)
                {
                    _isDeinitialized = true;
                    if (ReferenceEquals(_image, _ownedImage)) _image = null;
                    _ownedImage?.Dispose();
                    _ownedImage = null;
                }
            }
        }

        protected void RunEvery(
            string name,
            TimeSpan interval,
            Func<CancellationToken, Task> operation,
            bool runImmediately = true,
            CancellationToken cancellationToken = default)
        {
            PluginContext.RunEvery(
                GetType().DeclaringType?.Name ?? GetType().Name,
                _taskOwner,
                name,
                interval,
                operation,
                runImmediately,
                cancellationToken.CanBeCanceled ? cancellationToken : _cancellationToken);
        }

        protected void RunTask(
            string name,
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            PluginContext.RunTask(
                GetType().DeclaringType?.Name ?? GetType().Name,
                _taskOwner,
                name,
                operation,
                cancellationToken.CanBeCanceled ? cancellationToken : _cancellationToken);
        }

        public virtual async Task Init(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        public virtual async Task DeInit()
        {
            await Task.CompletedTask;
        }

        public virtual async Task OnTilePressDown(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        public virtual async Task OnTilePressUp(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
