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
using System;
using StreamDeckSharp;

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

            _configuration = _configurationProvider.LoadConfiguration();
            _configurationProvider.ConfigurationChanged += ConfigurationProvider_ConfigurationChanged;

            _fontCollection.AddSystemFonts();
            _fontCollection.Add("Fonts/Ubuntu-Bold.ttf");
            _fontCollection.Add("Fonts/Ubuntu-BoldItalic.ttf");
            _fontCollection.Add("Fonts/Ubuntu-Italic.ttf");
            _fontCollection.Add("Fonts/Ubuntu-Light.ttf");
            _fontCollection.Add("Fonts/Ubuntu-LightItalic.ttf");
            _fontCollection.Add("Fonts/Ubuntu-Medium.ttf");
            _fontCollection.Add("Fonts/Ubuntu-MediumItalic.ttf");
            _fontCollection.Add("Fonts/Ubuntu-Regular.ttf");

            _streamDeck = StreamDeck.OpenDevice().WithButtonPressEffect();
            _streamDeck.ClearKeys();
            _streamDeck.KeyStateChanged += StreamDeck_KeyStateChanged;
        }

        /// <summary>
        /// This handler is called when the configuration file is updated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ConfigurationProvider_ConfigurationChanged(object? sender, System.EventArgs e)
        {
            if (_applyingConfiguration) return;
            _applyingConfiguration = true;

            // TODO: Wait for file access ready in a better way
            await Task.Delay(500); // Wait for file to finish writing
            _configuration = _configurationProvider.LoadConfiguration();

            // DeInit all Tiles
            KeyWrappers.ForEach(x => x.CancellationTokenSource.Cancel());
            await Task.WhenAll(KeyWrappers
                .Where(x => x.Plugin != null)
                .Select(x => x.Tile?.DeInit() ?? Task.CompletedTask));
            KeyWrappers.Clear();

            // Apply new configuration and refresh current page
            await ApplyConfigurationAsync();
            RefreshPage();

            _applyingConfiguration = false;
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
                                       let keyWrapper = new KeyWrapper(_logger, profile, page, key, plugin)
                                       select keyWrapper)
            {
                keyWrapper.Updated += KeyWrapper_Updated;

                if (keyWrapper.Key.Image != null)
                {
                    try
                    {
                        keyWrapper.Image = Image.Load(keyWrapper.Key.Image);
                    }
                    catch (Exception)
                    {
                        keyWrapper.Image = Image.Load("Images/icon.png");
                    }
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
                        break;

                    case Key.KEY_TYPE_GO_BACK:
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
                        if (keyWrapper.Key.Profile == null || keyWrapper.Key.Page == null) return;
                        GotoPage(keyWrapper.Key.Profile, keyWrapper.Key.Page);
                        break;

                    case Key.KEY_TYPE_GO_BACK:
                        GotoPreviousPage();
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
                        keyWrapper.Tile?.TextColor ?? keyWrapper.Key.TextColorAsColor,
                        keyWrapper.Tile?.BackgroundColor ?? keyWrapper.Key.BackgroundColorAsColor,
                        keyWrapper.Image ?? keyWrapper.Tile?.Image,
                        keyWrapper.Tile?.ImagePadding ?? keyWrapper.Key.ImagePadding,
                        keyWrapper.Tile?.ShowIndicator,
                        keyWrapper.Tile?.IndicatorColor ?? keyWrapper.Key.ActivityIndicatorColorAsColor,
                        keyWrapper.Key.ShowFolderIndicator ??
                            (keyWrapper.Key.KeyType == Key.KEY_TYPE_GOTO_PAGE ? true : false),
                        keyWrapper.Key.FolderIndicatorColorAsColor ?? Color.Blue
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
            Color? indicatorColor = null,
            bool? folderIndicator = null,
            Color? folderIndicatorColor = null
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
                if (_fontCollection.TryGet(font ?? "Ubuntu", out var fontFamily))
                {
                    var f = fontFamily.CreateFont(fontSize ?? 16, FontStyle.Bold);

                    var size = TextMeasurer.Measure(text, new TextOptions(f));

                    var center = new PointF(_streamDeck.Keys.KeySize / 2, _streamDeck.Keys.KeySize / 2);
                    var textOptions = new TextOptions(f)
                    {
                        Origin = center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                    };
                    i.Mutate(x => x.DrawText(textOptions, text, textColor ?? Color.White));
                }
            }

            // Add indicator
            if (indicator.HasValue && indicator.Value)
            {
                i.Mutate(x => x.Fill(indicatorColor ?? Color.Yellow, new EllipsePolygon(_streamDeck.Keys.KeySize - 7, 7, 3)));
            }

            // Add folder indicator
            if (folderIndicator.HasValue && folderIndicator.Value)
            {
                var lineHeight = 5;
                i.Mutate(x => x.Fill(folderIndicatorColor ?? Color.Azure, new Rectangle(0, _streamDeck.Keys.KeySize - lineHeight, _streamDeck.Keys.KeySize, lineHeight)));
            }

            return i;
        }
    }
}
