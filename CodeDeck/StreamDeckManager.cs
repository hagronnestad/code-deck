using OpenMacroBoard.SDK;
using SixLabors.Fonts;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Linq;
using SixLabors.ImageSharp.Drawing;
using System.Collections.Generic;
using CodeDeck.Models.Configuration;
using Microsoft.Extensions.Logging;
using CodeDeck.PluginSystem;
using System.Threading.Tasks;

namespace CodeDeck
{
    public class StreamDeckManager
    {
        private readonly IMacroBoard _streamDeck;
        private readonly FontCollection _fontCollection = new();

        private readonly ILogger<StreamDeckManager> _logger;
        private readonly ConfigurationProvider _configurationProvider;
        public StreamDeckConfiguration _configuration;
        private readonly PluginLoader _pluginLoader;

        public List<KeyWrapper> KeyWrappers { get; set; } = new();

        public string? _previousProfileName = null;
        public string? _currentProfileName;

        public string? _previousPageName = null;
        public string? _currentPageName;

        private bool _applyingConfiguration = false;


        public StreamDeckManager(
            ILogger<StreamDeckManager> logger,
            ConfigurationProvider configurationProvider,
            PluginLoader pluginLoader
        )
        {
            _logger = logger;

            _configurationProvider = configurationProvider;
            _pluginLoader = pluginLoader;
            _configuration = configurationProvider.LoadConfiguration();

            configurationProvider.ConfigurationChanged += async (sender, e) => {
                if (_applyingConfiguration) return;
                _applyingConfiguration = true;
                
                await Task.Delay(500); // Wait for file to finish writing
                _configuration = configurationProvider.LoadConfiguration();
                
                KeyWrappers.Clear();
                await ApplyConfigurationAsync();
                RefreshPage();
                
                _applyingConfiguration = false;
            };

            _fontCollection.AddSystemFonts();

            _streamDeck = StreamDeckSharp.StreamDeck.OpenDevice();
            _streamDeck.ClearKeys();
            _streamDeck.KeyStateChanged += StreamDeck_KeyStateChanged;
        }

        public async Task ApplyConfigurationAsync()
        {
            _streamDeck.SetBrightness((byte)_configuration.Brightness);

            var profile = _configuration.Profiles.FirstOrDefault();
            if (profile == null)
            {
                _logger.LogError("No profiles!");
                return;
            }
            _currentProfileName ??= profile?.Name;

            var page = profile?.Pages?.FirstOrDefault();
            if (page == null)
            {
                _logger.LogError("No pages!");
                return;
            }
            _currentPageName ??= page?.Name;

            await CreateKeyWrappers();

            RefreshPage();
        }

        public async Task CreateKeyWrappers()
        {
            foreach (var keyWrapper in from profile in _configuration.Profiles
                                       from page in profile.Pages
                                       from key in page.Keys
                                       let plugin = _pluginLoader.LoadedPlugins.FirstOrDefault(x => x.Name == key.Plugin)
                                       let keyWrapper = new KeyWrapper(profile, page, key, plugin)
                                       select keyWrapper)
            {
                keyWrapper.Updated += KeyWrapper_Updated;

                if (keyWrapper.Key.Image != null)
                {
                    keyWrapper.Image = Image.Load(keyWrapper.Key.Image);
                }

                KeyWrappers.Add(keyWrapper);
            }

            await Task.WhenAll(KeyWrappers
                .Where(x => x.Plugin != null)
                .Select(x => x.InstantiateTileObjectAsync()));
        }

        public void RemoveKeyWrapper()
        {
            foreach (var keyWrapper in KeyWrappers)
            {
                keyWrapper.Updated -= KeyWrapper_Updated;
                KeyWrappers.Remove(keyWrapper);
            }
        }

        public void GotoPage(string profileName, string pageName)
        {
            var profile = _configuration.Profiles.FirstOrDefault(x => x.Name == profileName);
            var page = profile?.Pages.FirstOrDefault(x => x.Name == pageName);

            if (profile != null && page != null)
            {
                _previousProfileName = _currentProfileName;
                _currentProfileName = profile.Name;
                _previousPageName = _currentPageName;
                _currentPageName = page.Name;

                RefreshPage();
            }
        }

        public void RefreshPage()
        {
            _streamDeck.ClearKeys();

            foreach (var keyWrapper in KeyWrappers.Where(x => x.Page.Name == _currentPageName))
            {
                UpdateKeyBitmap(keyWrapper);
            }
        }

        public void GotoPreviousPage()
        {
            if (_previousProfileName == null || _previousPageName == null) return;
            GotoPage(_previousProfileName, _previousPageName);
        }

        private void StreamDeck_KeyStateChanged(object? sender, KeyEventArgs e)
        {
            var keyWrapper = KeyWrappers
                .Where(x => x.Profile.Name == _currentProfileName)
                .Where(x => x.Page.Name == _currentPageName)
                .Where(x => x.Key.Index == e.Key)
                .FirstOrDefault();

            if (keyWrapper == null) return;

            if (e.IsDown)
            {
                switch (keyWrapper.Key.KeyType)
                {
                    case Key.KEY_TYPE_GOTO_PAGE:
                        if (keyWrapper.Key.Profile == null || keyWrapper.Key.Page == null) return;
                        GotoPage(keyWrapper.Key.Profile, keyWrapper.Key.Page);
                        break;

                    case Key.KEY_TYPE_GO_BACK:
                        GotoPreviousPage();
                        break;

                    case Key.KEY_TYPE_NORMAL:
                        keyWrapper.HandleKeyPressDown();
                        break;
                }
            }
            else
            {
                switch (keyWrapper.Key.KeyType)
                {
                    case Key.KEY_TYPE_GOTO_PAGE:
                        break;

                    case Key.KEY_TYPE_GO_BACK:
                        break;

                    case Key.KEY_TYPE_NORMAL:
                        keyWrapper.HandleKeyPressUp();
                        break;
                }
            }
        }

        /// <summary>
        /// Event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="keyWrapper"></param>
        private void KeyWrapper_Updated(object? sender, KeyWrapper keyWrapper)
        {
            if (_currentProfileName != keyWrapper.Profile.Name || _currentPageName != keyWrapper.Page.Name) return;

            UpdateKeyBitmap(keyWrapper);
        }

        public void ClearKeys()
        {
            _streamDeck?.ClearKeys();
        }

        public void UpdateKeyBitmap(KeyWrapper keyWrapper)
        {
            _streamDeck.SetKeyBitmap(keyWrapper.Key.Index,
                KeyBitmap.Create.FromImageSharpImage(
                    CreateTileBitmap(
                        keyWrapper.Tile?.Text ?? keyWrapper.Key.Text,
                        keyWrapper.Tile?.Font,
                        keyWrapper.Tile?.FontSize,
                        keyWrapper.Tile?.TextColor,
                        keyWrapper.Tile?.BackgroundColor,
                        keyWrapper.Image ?? keyWrapper.Tile?.Image,
                        keyWrapper.Tile?.ImagePadding,
                        keyWrapper.Tile?.ShowIndicator,
                        keyWrapper.Tile?.IndicatorColor
                    )));
        }

        private Image CreateTileBitmap(
            string? text,
            string? font = null,
            float? fontSize = null,
            Color? textColor = null,
            Color? bgColor = null,
            Image? image = null,
            int? imagePadding = null,
            bool? indicator = null,
            Color? indicatorColor = null
        )
        {
            var i = new Image<Rgba32>(_streamDeck.Keys.KeySize, _streamDeck.Keys.KeySize);

            // Add background color
            i.Mutate(x => x.BackgroundColor(bgColor ?? Color.Transparent));

            // Add image
            if (image != null)
            {
                int padding = imagePadding ?? 0;

                image.Mutate(i => i.Resize(_streamDeck.Keys.KeySize - (padding * 2),
                    _streamDeck.Keys.KeySize - (padding * 2)));
                i.Mutate(x => x.DrawImage(image, new Point(padding, padding), 1f));
            }

            // Add text
            if (text != null && !string.IsNullOrWhiteSpace(text))
            {
                var fontFamily = _fontCollection.Get(font ?? "Arial");
                var f = fontFamily.CreateFont(fontSize ?? 16, FontStyle.Bold);
                FontRectangle size = TextMeasurer.Measure(text, new TextOptions(f));

                i.Mutate(x => x.DrawText(text, f, textColor ?? Color.White,
                    new PointF(_streamDeck.Keys.KeySize / 2 - size.Width / 2,
                    _streamDeck.Keys.KeySize / 2 - size.Height / 2)));
            }

            if (indicator.HasValue && indicator.Value)
            {
                i.Mutate(x => x.Fill(indicatorColor ?? Color.Yellow, new EllipsePolygon(_streamDeck.Keys.KeySize - 5, 5, 3)));
            }

            return i;
        }
    }
}
