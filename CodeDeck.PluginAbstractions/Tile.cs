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
            set
            {
                _text = value;
                NotifyChange?.Invoke();
            }
        }

        private Color? _textColor;
        public Color? TextColor
        {
            get => _textColor; set
            {
                _textColor = value;
                NotifyChange?.Invoke();
            }
        }

        private Color? _bgColor;
        public Color? BackgroundColor
        {
            get => _bgColor; set
            {
                _bgColor = value;
                NotifyChange?.Invoke();
            }
        }

        private string? _font;
        public string? Font
        {
            get => _font; set
            {
                _font = value;
                NotifyChange?.Invoke();
            }
        }

        private float? _fontSize;
        public float? FontSize
        {
            get => _fontSize; set
            {
                _fontSize = value;
                NotifyChange?.Invoke();
            }
        }

        private Image? _image;
        public Image? Image
        {
            get => _image; set
            {
                _image = value;
                NotifyChange?.Invoke();
            }
        }

        private int? _imagePadding;
        public int? ImagePadding
        {
            get => _imagePadding; set
            {
                _imagePadding = value;
                NotifyChange?.Invoke();
            }
        }

        private bool? _showIndicator;
        public bool? ShowIndicator
        {
            get => _showIndicator; set
            {
                _showIndicator = value;
                NotifyChange?.Invoke();
            }
        }

        private Color? _indicatorColor;
        public Color? IndicatorColor
        {
            get => _indicatorColor; set
            {
                _indicatorColor = value;
                NotifyChange?.Invoke();
            }
        }

        public Dictionary<string, string>? Settings { get; set; }


        public Tile()
        {

        }

        public virtual async Task Init()
        {
            await Task.CompletedTask;
        }

        public virtual async Task DeInit()
        {
            await Task.CompletedTask;
        }

        public virtual async Task OnTilePressDown()
        {
            await Task.CompletedTask;
        }

        public virtual async Task OnTilePressUp()
        {
            await Task.CompletedTask;
        }
    }
}
