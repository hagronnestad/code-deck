using CodeDeck.Models.Configuration;
using CodeDeck.PluginSystem;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using OpenMacroBoard.SDK;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StreamDeckSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeDeck
{
    public class StreamDeckManager
    {
        private readonly ILogger<StreamDeckManager> _logger;
        private readonly ConfigurationProvider _configurationProvider;
        public StreamDeckConfiguration _configuration;
        private readonly IMacroBoard _streamDeck;
        private readonly PluginLoader _pluginLoader;
        private readonly List<KeyWrapper> _keyWrappers = new();
        private readonly FontCollection _fontCollection = new();
        private readonly Stack<(string profileName, string pageName)> _navigationStack = new();
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

            var sd = OpenStreamDeck();

            if (sd is null)
            {
                _logger.LogError($"{nameof(OpenStreamDeck)} failed!");
                Environment.Exit(1);
            }

            _streamDeck = sd;
            _streamDeck.ConnectionStateChanged += StreamDeck_ConnectionStateChanged;
            _streamDeck.KeyStateChanged += StreamDeck_KeyStateChanged;

            // Handle Lock/UnLock on Windows
            if (OperatingSystem.IsWindows())
            {
                SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            }
        }

        private IMacroBoard? OpenStreamDeck()
        {
            IMacroBoard? streamDeck;

            try
            {
                var availableDevices = StreamDeck.EnumerateDevices();
                if (availableDevices is null)
                {
                    _logger.LogError("No Stream Deck hardware found!");
                    return null;
                }

                // Log all available devices
                _logger.LogInformation($"Enumerated Stream Deck devices [{availableDevices.Count()}]:");
                foreach (var device in availableDevices)
                {
                    _logger.LogInformation($"Name: '{device.DeviceName}'; DevicePath: '{device.DevicePath}'; Keys: '{device.Keys.Count}'");
                }

                // Open specified or default device
                if (_configuration.DevicePath is not null)
                {
                    streamDeck = StreamDeck.OpenDevice(_configuration.DevicePath).WithButtonPressEffect();
                }
                else
                {
                    streamDeck = StreamDeck.OpenDevice().WithButtonPressEffect();
                }

                streamDeck.ClearKeys();
                streamDeck.SetBrightness((byte)_configuration.Brightness);
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception in '{nameof(ApplyConfigurationAsync)}'. Message: '{e.Message}'. This may indicate that an invalid '{nameof(StreamDeckConfiguration.DevicePath)}' was specified.");
                return null;
            }

            return streamDeck;
        }

        private void StreamDeck_ConnectionStateChanged(object? sender, ConnectionEventArgs e)
        {
            if (_streamDeck is null) return;
            _streamDeck.ClearKeys();
            _streamDeck.SetBrightness((byte)_configuration.Brightness);
            RefreshPage();
        }

        private void StreamDeck_KeyStateChanged(object? sender, KeyEventArgs e)
        {
            if (_navigationStack.Count == 0) return;
            var (profileName, pageName) = _navigationStack.Peek();

            var keyWrapper = _keyWrappers
                .Where(x => x.Profile.Name == profileName)
                .Where(x => x.Page.Name == pageName)
                .Where(x => x.Key.Index == e.Key)
                .FirstOrDefault();

            if (keyWrapper == null) return;

            switch (keyWrapper.Key.KeyType)
            {
                case Key.KEY_TYPE_GOTO_PAGE:
                    if (!e.IsDown)
                    {
                        if (keyWrapper.Key.Profile == null || keyWrapper.Key.Page == null) return;
                        GotoPage(keyWrapper.Key.Profile, keyWrapper.Key.Page);
                        break;
                    }
                    break;

                case Key.KEY_TYPE_GO_BACK:
                    if (!e.IsDown) GotoPreviousPage();
                    break;

                case Key.KEY_TYPE_NORMAL:
                    if (e.IsDown) keyWrapper.HandleKeyPressDown();
                    if (!e.IsDown) keyWrapper.HandleKeyPressUp();
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// This event handler switches profiles based on lock/unlock events on Windows
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (!OperatingSystem.IsWindows()) return;

            var lockScreenProfile = _configuration.Profiles
                .FirstOrDefault(x => x.ProfileType == Profile.PROFILE_TYPE_LOCK_SCREEN);

            var lockScreenPage = lockScreenProfile?.Pages
                .FirstOrDefault();

            if (lockScreenProfile is null || lockScreenPage is null) return;

            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock:
                    _navigationStack.Push((lockScreenProfile.Name, lockScreenPage.Name));
                    RefreshPage();
                    break;

                case SessionSwitchReason.SessionUnlock:
                    _navigationStack.Pop();
                    RefreshPage();
                    break;

                default:
                    break;
            }
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
            _keyWrappers.ForEach(x => x.CancellationTokenSource.Cancel());
            await Task.WhenAll(_keyWrappers
                .Where(x => x.Plugin != null)
                .Select(x => x.Tile?.DeInit() ?? Task.CompletedTask));
            _keyWrappers.Clear();

            // Apply new configuration and refresh current page
            await ApplyConfigurationAsync();

            _applyingConfiguration = false;
        }

        public async Task ApplyConfigurationAsync()
        {
            ClearKeys();

            _streamDeck?.SetKeyBitmap(_streamDeck.Keys.CountX / 2,
                KeyBitmap.Create.FromImageSharpImage(Image.Load("Images/icon.png")));

            var profile = _configuration.Profiles
                .FirstOrDefault(x => x.ProfileType == Profile.PROFILE_TYPE_NORMAL);

            if (profile == null)
            {
                _logger.LogError("No profiles!");
                return;
            }

            var page = profile.Pages?.FirstOrDefault();
            if (page == null)
            {
                _logger.LogError("No pages!");
                return;
            }

            if (_navigationStack.Count == 0) _navigationStack.Push((profile.Name, page.Name));

            await CreateKeyWrappers();

            RefreshPage();
        }

        public async Task CreateKeyWrappers()
        {
            // Instantiate all wrappers based on configuration
            var keyWrappers = (from profile in _configuration.Profiles
                               from page in profile.Pages
                               from key in page.Keys
                               let plugin = _pluginLoader.LoadedPlugins.FirstOrDefault(x => x.Name == key.Plugin)
                               let keyWrapper = new KeyWrapper(_logger, profile, page, key, plugin)
                               orderby keyWrapper.Key.Index
                               select keyWrapper)
                               .ToList();

            // Try to load the image specified for a key
            // This must be done here because not all keys are associated with a tile
            keyWrappers.ForEach(x =>
            {
                if (x.Key.Image != null) x.Image = SafeLoadImage(x.Key.Image, true);
            });

            // Instantiate all tiles in parallell and wait for all to finish
            await Task.WhenAll(keyWrappers
                .Where(x => x.Plugin != null)
                .Select(x => x.InstantiateTileObjectAsync()));

            // Bind all event handlers last to prevent unnecessary
            // key updates during init and draw all keys at the same time
            keyWrappers.ForEach(x => x.Updated += KeyWrapper_Updated);
            RefreshPage();

            // Keep wrappers for later
            _keyWrappers.AddRange(keyWrappers);
        }

        public void RemoveKeyWrapper()
        {
            foreach (var keyWrapper in _keyWrappers)
            {
                keyWrapper.Updated -= KeyWrapper_Updated;
                _keyWrappers.Remove(keyWrapper);
            }
        }

        /// <summary>
        /// Safely (without exceptions) load an image or a default placeholder
        /// if the image file does not exist or can't be loaded for some reason.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="usePlaceholder"></param>
        /// <returns></returns>
        private Image? SafeLoadImage(string fileName, bool usePlaceholder = false)
        {
            try
            {
                return Image.Load(fileName);
            }
            catch (Exception)
            {
                _logger.LogWarning($"<{nameof(SafeLoadImage)}>: tried to load image: '{fileName}'");
                if (usePlaceholder) return Image.Load("Images/icon.png");
            }

            return null;
        }

        public void GotoPage(string profileName, string pageName)
        {
            var profile = _configuration.Profiles.FirstOrDefault(x => x.Name == profileName);
            var page = profile?.Pages.FirstOrDefault(x => x.Name == pageName);

            if (profile != null && page != null)
            {
                _navigationStack.Push((profile.Name, page.Name));
                RefreshPage();
            }
        }

        public void RefreshPage()
        {
            _streamDeck.ClearKeys();
            var (profileName, pageName) = _navigationStack.Peek();

            foreach (var keyWrapper in _keyWrappers
                .Where(x => x.Profile.Name == profileName)
                .Where(x => x.Page.Name == pageName))
            {
                UpdateKeyBitmap(keyWrapper);
            }
        }

        public void GotoPreviousPage()
        {
            if (_navigationStack.Count < 2) return;
            _navigationStack.Pop();
            var (profileName, pageName) = _navigationStack.Pop();
            GotoPage(profileName, pageName);
        }

        /// <summary>
        /// Event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="keyWrapper"></param>
        private void KeyWrapper_Updated(object? sender, KeyWrapper keyWrapper)
        {
            if (_navigationStack.Count == 0) return;
            var (profileName, pageName) = _navigationStack.Peek();
            if (profileName != keyWrapper.Profile.Name || pageName != keyWrapper.Page.Name) return;

            UpdateKeyBitmap(keyWrapper);
        }

        public void ClearKeys()
        {
            _streamDeck?.ClearKeys();
        }

        public void UpdateKeyBitmap(KeyWrapper keyWrapper)
        {
            _streamDeck?.SetKeyBitmap(keyWrapper.Key.Index,
                KeyBitmap.Create.FromImageSharpImage(CreateTileBitmap(keyWrapper)));
        }

        private Image CreateTileBitmap(KeyWrapper keyWrapper)
        {
            // TODO: This method is a mess and should be massively optimized
            // - KeyWrapper should cache the key image and use the cached version on page refresh
            // - The created bitmap object (`i`) should probably be reused when redrawing
            // - Fonts should not be recreated on every draw

            // Get customized values, Key (set by user) values override Tile values, set default value if needed
            string? text = keyWrapper.Key.Text ?? keyWrapper.Tile?.Text;
            string? font = keyWrapper.Key.Font ?? keyWrapper.Tile?.Font ?? "Ubuntu";
            bool fontBold = keyWrapper.Key.FontBold ?? false;
            bool fontItalic = keyWrapper.Key.FontItalic ?? false;
            float fontSize = keyWrapper.Key.FontSize ?? keyWrapper.Tile?.FontSize ?? 16.0f;
            float lineSpacing = keyWrapper.Key.LineSpacing ?? 1.1f;
            Color textColor = keyWrapper.Key.TextColorAsColor ?? keyWrapper.Tile?.TextColor ?? Color.White;
            Color bgColor = keyWrapper.Key.BackgroundColorAsColor ?? keyWrapper.Tile?.BackgroundColor ?? Color.Transparent;
            Image? image = keyWrapper.Image ?? keyWrapper.Tile?.Image;
            int imagePadding = keyWrapper.Key.ImagePadding ?? keyWrapper.Tile?.ImagePadding ?? 0;
            bool? indicator = keyWrapper.Tile?.ShowIndicator;
            Color indicatorColor = keyWrapper.Key.ActivityIndicatorColorAsColor ?? keyWrapper.Tile?.IndicatorColor ?? Color.Yellow;
            bool? folderIndicator = keyWrapper.Key.ShowFolderIndicator ?? (keyWrapper.Key.KeyType == Key.KEY_TYPE_GOTO_PAGE);
            Color folderIndicatorColor = keyWrapper.Key.FolderIndicatorColorAsColor ?? Color.Blue;

            // Create key bitmap
            var i = new Image<Rgba32>(_streamDeck.Keys.KeySize, _streamDeck.Keys.KeySize);

            // Add background color
            i.Mutate(x => x.BackgroundColor(bgColor));

            // Add image
            if (image != null)
            {
                image.Mutate(i => i.Resize(_streamDeck.Keys.KeySize - (imagePadding * 2),
                    _streamDeck.Keys.KeySize - (imagePadding * 2)));
                i.Mutate(x => x.DrawImage(image, new Point(imagePadding, imagePadding), 1f));
            }

            // Add text
            if (text != null && !string.IsNullOrWhiteSpace(text))
            {
                if (_fontCollection.TryGet(font, out var fontFamily))
                {
                    var fontStyle = FontStyle.Regular;
                    if (fontBold) fontStyle |= FontStyle.Bold;
                    if (fontItalic) fontStyle |= FontStyle.Italic;

                    var f = fontFamily.CreateFont(fontSize, fontStyle);

                    var center = new PointF(_streamDeck.Keys.KeySize / 2, _streamDeck.Keys.KeySize / 2);
                    var textOptions = new TextOptions(f)
                    {
                        Origin = center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        LineSpacing = lineSpacing
                    };

                    if (_configuration.FallbackFont is not null &&
                        _fontCollection.TryGet(_configuration.FallbackFont, out var fallBackFontFamily))
                    {
                        textOptions.FallbackFontFamilies = new[] { fallBackFontFamily };
                    }

                    i.Mutate(x => x.DrawText(textOptions, text, textColor));
                }
            }

            // Add indicator
            if (indicator.HasValue && indicator.Value)
            {
                i.Mutate(x => x.Fill(indicatorColor, new EllipsePolygon(_streamDeck.Keys.KeySize - 7, 7, 3)));
            }

            // Add folder indicator
            if (folderIndicator.HasValue && folderIndicator.Value)
            {
                var lineHeight = 5;
                i.Mutate(x => x.Fill(folderIndicatorColor,
                    new Rectangle(0, _streamDeck.Keys.KeySize - lineHeight, _streamDeck.Keys.KeySize, lineHeight)));
            }

            return i;
        }
    }
}
