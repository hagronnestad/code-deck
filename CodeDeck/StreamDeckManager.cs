using CodeDeck.Models.Configuration;
using CodeDeck.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using OpenMacroBoard.SDK;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using StreamDeckSharp;
using System.Collections.Concurrent;

namespace CodeDeck
{
    public class StreamDeckManager
    {
        private record NavigationItem(string ProfileName, string PageName);

        private readonly ILogger<StreamDeckManager> _logger;
        private readonly ConfigurationProvider _configurationProvider;
        private readonly ProcessMonitor _processMonitor;
        private readonly IMacroBoard _streamDeck;
        private readonly PluginLoader _pluginLoader;
        private IReadOnlyList<KeyWrapper> _keyWrappers = Array.Empty<KeyWrapper>();
        private readonly object _keyWrappersLock = new();
        private readonly FontCollection _fontCollection = new();
        private readonly ConcurrentDictionary<(string, float, FontStyle), Font> _fontCache = new();
        private readonly Stack<NavigationItem> _navigationStack = new();
        private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
        private NavigationItem? _currentPage;
        private int _stopping;


        public StreamDeckManager(
            ILogger<StreamDeckManager> logger,
            ConfigurationProvider configurationProvider,
            ProcessMonitor processMonitor,
            PluginLoader pluginLoader
        )
        {
            _logger = logger;
            _configurationProvider = configurationProvider;
            _processMonitor = processMonitor;
            _pluginLoader = pluginLoader;

            _configurationProvider.ConfigurationChanged += ConfigurationProvider_ConfigurationChanged;
            _pluginLoader.PluginsChanged += PluginLoader_PluginsChanged;

            _fontCollection.AddSystemFonts();
            _fontCollection.Add("Fonts/Twemoji.Mozilla.ttf");
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

            _processMonitor.ProcessStarted += ProcessMonitor_ProcessStarted;
            _processMonitor.ProcessExited += ProcessMonitor_ProcessExited;

            // Handle Lock/UnLock on Windows
            if (OperatingSystem.IsWindows())
            {
                SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            }
        }

        private void ProcessMonitor_ProcessStarted(object? sender, string e)
        {
            var flatKeyConfiguration = _configurationProvider.LoadedFlatConfiguration
                .FirstOrDefault(x => string.Equals(
                    x.Page.ProcessStartedTrigger,
                    e,
                    StringComparison.OrdinalIgnoreCase));

            if (flatKeyConfiguration is null) return;

            NavigateToPage(flatKeyConfiguration.Profile.Name, flatKeyConfiguration.Page.Name);
        }

        private void ProcessMonitor_ProcessExited(object? sender, string e)
        {
            if (_currentPage is null) return;

            var flatKeyConfiguration = _configurationProvider.LoadedFlatConfiguration
                .FirstOrDefault(x => string.Equals(
                    x.Page.ProcessStartedTrigger,
                    e,
                    StringComparison.OrdinalIgnoreCase));
            if (flatKeyConfiguration is null) return;

            // If ProcessStartedTriggerNavigatePreviousOnExit is true and the current page is the
            // same page that was navigated to by ProcessStartedTrigger then navigate to the previous page
            if (flatKeyConfiguration.Page.ProcessStartedTriggerNavigatePreviousOnExit
                && _currentPage.ProfileName == flatKeyConfiguration.Profile.Name
                && _currentPage.PageName == flatKeyConfiguration.Page.Name)
            {
                NavigateToPreviousPage();
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
                if (_configurationProvider.LoadedConfiguration.DevicePath is not null)
                {
                    streamDeck = StreamDeck.OpenDevice(_configurationProvider.LoadedConfiguration.DevicePath)
                        .WithButtonPressEffect();
                }
                else
                {
                    streamDeck = StreamDeck.OpenDevice().WithButtonPressEffect();
                }

                streamDeck.ClearKeys();
                streamDeck.SetBrightness((byte)_configurationProvider.LoadedConfiguration.Brightness);
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception in '{nameof(ApplyConfiguration)}'. Message: '{e.Message}'. This may indicate that an invalid '{nameof(StreamDeckConfiguration.DevicePath)}' was specified.");
                return null;
            }

            return streamDeck;
        }

        private void StreamDeck_ConnectionStateChanged(object? sender, ConnectionEventArgs e)
        {
            _logger.LogWarning($"{nameof(StreamDeck_ConnectionStateChanged)}: {nameof(e.NewConnectionState)}: {e.NewConnectionState}");

            if (_streamDeck is null) return;
            _streamDeck.SetBrightness((byte)_configurationProvider.LoadedConfiguration.Brightness);
            RefreshPage();
        }

        private void StreamDeck_KeyStateChanged(object? sender, KeyEventArgs e)
        {
            var keyWrapper = KeyWrappers
                .Where(x => x.IsShowing)
                .Where(x => x.Key.Index == e.Key)
                .FirstOrDefault();

            if (keyWrapper == null) return;

            switch (keyWrapper.Key.KeyType)
            {
                case Key.KEY_TYPE_GO_BACK:
                    if (!e.IsDown) NavigateToPreviousPage();
                    break;

                case Key.KEY_TYPE_NORMAL:
                    if (e.IsDown) keyWrapper.HandleKeyPressDown();
                    if (!e.IsDown) keyWrapper.HandleKeyPressUp();

                    // Perform navigation if a profile and page has been set
                    if (!e.IsDown && keyWrapper.Key.Profile != null && keyWrapper.Key.Page != null)
                    {
                        NavigateToPage(keyWrapper.Key.Profile, keyWrapper.Key.Page);
                    }

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

            var lockScreenProfile = _configurationProvider.LoadedConfiguration.Profiles
                .FirstOrDefault(x => x.ProfileType == Profile.PROFILE_TYPE_LOCK_SCREEN);

            var lockScreenPage = lockScreenProfile?.Pages
                .FirstOrDefault();

            if (lockScreenProfile is null || lockScreenPage is null) return;

            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock:
                    NavigateToPage(lockScreenProfile.Name, lockScreenPage.Name);
                    break;

                case SessionSwitchReason.SessionUnlock:
                    NavigateToPreviousPage();
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
        private async void ConfigurationProvider_ConfigurationChanged(object? sender, EventArgs e)
        {
            try
            {
                await ReapplyPluginsAndConfiguration();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to apply the updated configuration.");
            }
        }

        private async void PluginLoader_PluginsChanged(object? sender, IReadOnlyCollection<string> pluginNames)
        {
            try
            {
                await ReloadPlugins(pluginNames);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to reload changed plugins.");
            }
        }

        private async Task ReapplyPluginsAndConfiguration()
        {
            if (!await TryEnterLifecycleAsync()) return;

            try
            {
                _processMonitor.Stop();
                await DestroyKeyWrappers();
                await _pluginLoader.LoadAllPluginsAsync();
                await ApplyConfiguration();
                _processMonitor.Start();
            }
            finally
            {
                _lifecycleLock.Release();
            }
        }

        private async Task ReloadPlugins(IReadOnlyCollection<string> pluginNames)
        {
            if (!await TryEnterLifecycleAsync()) return;

            try
            {
                var reloadResults = await Task.WhenAll(pluginNames
                    .Select(_pluginLoader.TryReloadPluginAsync));

                var reloadedPluginNames = reloadResults
                    .OfType<PluginReloadResult>()
                    .Select(result => result.PluginName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (!reloadedPluginNames.Any()) return;

                var previousPlugins = reloadResults
                    .OfType<PluginReloadResult>()
                    .Select(result => result.PreviousPlugin)
                    .Where(plugin => plugin is not null)
                    .Cast<Plugin>()
                    .ToHashSet();

                previousPlugins.ToList().ForEach(plugin => plugin.Cancel());

                // Select by plugin name rather than by Plugin instance, so that key
                // wrappers created while the plugin was unavailable (Plugin == null)
                // are also destroyed instead of being duplicated below.
                var affectedKeyWrappers = KeyWrappers
                    .Where(keyWrapper => keyWrapper.Key.Plugin is not null
                        && reloadedPluginNames.Contains(keyWrapper.Key.Plugin))
                    .ToList();

                await DestroyKeyWrappers(affectedKeyWrappers);
                await Task.WhenAll(previousPlugins.Select(plugin => plugin.UnloadPluginAsync()));
                _logger.LogInformation($"Unloaded {previousPlugins.Count} replaced plugin(s).");

                var affectedConfigurations = _configurationProvider.LoadedFlatConfiguration
                    .Where(configuration => configuration.Key.Plugin is not null
                        && reloadedPluginNames.Contains(configuration.Key.Plugin));

                await CreateKeyWrappers(affectedConfigurations);
                RefreshPage();
            }
            finally
            {
                _lifecycleLock.Release();
            }
        }

        public async Task Start()
        {
            // Held for the whole startup so that plugin or configuration change events
            // (the file system watchers are already armed at this point) cannot run a
            // reload concurrently with the initial load.
            await _lifecycleLock.WaitAsync();
            try
            {
                await _pluginLoader.LoadAllPluginsAsync();
                await ApplyConfiguration();
                _processMonitor.Start();
            }
            finally
            {
                _lifecycleLock.Release();
            }
        }

        public async Task ApplyConfiguration()
        {
            ClearKeys();

            // Show Code Deck icon while applying configuration
            _streamDeck?.SetKeyBitmap(_streamDeck.Keys.CountX / 2,
                KeyBitmap.Create.FromImageSharpImage(Image.Load("Images/icon.png")));

            // Clear and update monitored processes based on configuration
            _processMonitor.Clear();
            _configurationProvider.LoadedFlatConfiguration
                .Select(x => x.Page)
                .Distinct()
                .Select(x => x.ProcessStartedTrigger)
                .OfType<string>()
                .ToList()
                .ForEach(x => _processMonitor.Add(x));

            // Get first normal profile
            var profile = _configurationProvider.LoadedConfiguration.Profiles
                .FirstOrDefault(x => x.ProfileType == Profile.PROFILE_TYPE_NORMAL);

            if (profile == null)
            {
                _logger.LogError("No profiles!");
                return;
            }

            // Get first page
            var page = profile.Pages?.FirstOrDefault();
            if (page == null)
            {
                _logger.LogError("No pages!");
                return;
            }

            await CreateKeyWrappers();

            if (_navigationStack.Count == 0)
            {
                // Navigate to first page if no page exists on the navigation stack
                NavigateToPage(profile.Name, page.Name);
            }
            else
            {
                // Refresh page when live reloading configuration
                RefreshPage();
            }
        }

        public Task CreateKeyWrappers()
        {
            return CreateKeyWrappers(_configurationProvider.LoadedFlatConfiguration);
        }

        private async Task CreateKeyWrappers(IEnumerable<Models.FlatKeyConfiguration> flatKeyConfigurations)
        {
            // Instantiate all wrappers based on configuration
            var keyWrappers = (
                from flatKeyConfiguration in flatKeyConfigurations
                let plugin = _pluginLoader.LoadedPlugins.FirstOrDefault(x => x.Name == flatKeyConfiguration.Key.Plugin)
                let keyWrapper = new KeyWrapper(_logger, flatKeyConfiguration.Profile, flatKeyConfiguration.Page, flatKeyConfiguration.Key, plugin)
                orderby keyWrapper.Key.Index
                select keyWrapper
            ).ToList();

            // Try to load the image specified for a key
            // This must be done here because not all keys are associated with a tile
            await Task.WhenAll(keyWrappers
                .Where(keyWrapper => keyWrapper.Key.Image is not null)
                .Select(async keyWrapper =>
                {
                    keyWrapper.Image = await Task.Run(() => SafeLoadImage(keyWrapper.Key.Image!, true));
                }));

            // Publish wrappers before initialization so each tile can finish in its own
            // time and redraw only its own key without delaying the rest of the deck.
            keyWrappers.ForEach(x => x.Updated += KeyWrapper_Updated);
            AddKeyWrappers(keyWrappers);
            keyWrappers.ForEach(keyWrapper => keyWrapper.StartInitialization());
        }

        /// <summary>
        /// The current key wrappers. Readers must enumerate the returned snapshot,
        /// which is swapped atomically by <see cref="AddKeyWrappers"/> and
        /// <see cref="RemoveKeyWrappers"/> so device and navigation threads never
        /// observe a partially updated collection.
        /// </summary>
        private IReadOnlyList<KeyWrapper> KeyWrappers => Volatile.Read(ref _keyWrappers);

        private void AddKeyWrappers(IEnumerable<KeyWrapper> keyWrappers)
        {
            lock (_keyWrappersLock)
            {
                Volatile.Write(ref _keyWrappers, _keyWrappers.Concat(keyWrappers).ToArray());
            }
        }

        private void RemoveKeyWrappers(IEnumerable<KeyWrapper> keyWrappers)
        {
            var removed = keyWrappers.ToHashSet();

            lock (_keyWrappersLock)
            {
                Volatile.Write(ref _keyWrappers,
                    _keyWrappers.Where(keyWrapper => !removed.Contains(keyWrapper)).ToArray());
            }
        }

        public async Task DestroyKeyWrappers()
        {
            await DestroyKeyWrappers(KeyWrappers.ToList());
        }

        private async Task DestroyKeyWrappers(List<KeyWrapper> keyWrappers)
        {
            if (!keyWrappers.Any()) return;

            _logger.LogInformation($"Deinitializing {keyWrappers.Count} tile(s).");
            keyWrappers.ForEach(keyWrapper =>
            {
                keyWrapper.Updated -= KeyWrapper_Updated;
                keyWrapper.Cancel();
            });

            await Task.WhenAll(keyWrappers
                .Select(keyWrapper => keyWrapper.InitializationTask));

            await Task.WhenAll(keyWrappers
                .Select(keyWrapper => keyWrapper.DeinitializeAsync()));

            keyWrappers.ForEach(keyWrapper => keyWrapper.Dispose());
            RemoveKeyWrappers(keyWrappers);
        }

        public async Task StopAsync()
        {
            if (Interlocked.Exchange(ref _stopping, 1) != 0) return;

            _logger.LogInformation("Stopping Stream Deck manager.");

            // Cancel before taking the lifecycle lock: a reload in progress may be
            // blocked inside a tile's Init until its token is cancelled, and would
            // otherwise never release the lock.
            _processMonitor.Stop();
            _pluginLoader.CancelAllPlugins();

            await _lifecycleLock.WaitAsync();

            try
            {
                await DestroyKeyWrappers();
                await _pluginLoader.UnloadAllPluginsAsync();
                ClearKeys();
                _logger.LogInformation("Stream Deck manager stopped.");
            }
            finally
            {
                _lifecycleLock.Release();
            }
        }

        private async Task<bool> TryEnterLifecycleAsync()
        {
            if (Volatile.Read(ref _stopping) != 0) return false;
            await _lifecycleLock.WaitAsync();

            if (Volatile.Read(ref _stopping) == 0) return true;

            _lifecycleLock.Release();
            return false;
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


        public void NavigateToPage(string profileName, string pageName)
        {
            // Make sure the profile and page exists in the configuration
            var profile = _configurationProvider.LoadedConfiguration.Profiles.FirstOrDefault(x => x.Name == profileName);
            var page = profile?.Pages.FirstOrDefault(x => x.Name == pageName);
            if (profile == null || page == null) return;

            // Prevent navigating to the same page as the current page
            if (_currentPage is not null)
            {
                if (_currentPage.ProfileName == profile.Name && _currentPage.PageName == page.Name) return;
            }

            // Update navigation stack and current page
            var navigationItem = new NavigationItem(profile.Name, page.Name);
            _navigationStack.Push(navigationItem);
            _currentPage = navigationItem;

            RefreshPage();
        }

        public void NavigateToPreviousPage()
        {
            // Return if there is no page to navigate back to
            if (_navigationStack.Count < 2) return;

            // Pop the current page of the navigation stack
            _navigationStack.Pop();

            // Pop the previous page of the navigation stack
            var previousPage = _navigationStack.Pop();

            // Navigate to the previous page
            NavigateToPage(previousPage.ProfileName, previousPage.PageName);
        }

        public void RefreshPage()
        {
            _streamDeck.ClearKeys();

            if (_currentPage is null) return;

            // Update IsShowing property
            var keyWrappers = KeyWrappers;
            foreach (var k in keyWrappers)
            {
                k.IsShowing = k.Profile.Name == _currentPage.ProfileName && k.Page.Name == _currentPage.PageName;
            }

            var keyWrappersForCurrentPage = keyWrappers.Where(x => x.IsShowing).ToList();
            if (!keyWrappersForCurrentPage.Any()) return;

            foreach (var keyWrapper in keyWrappersForCurrentPage)
            {
                keyWrapper.CachedComposedKeyBitmapImage = CreateKeyBitmap(keyWrapper);
                _streamDeck?.SetKeyBitmap(keyWrapper.Key.Index, keyWrapper.CachedComposedKeyBitmapImage);
            }
        }

        /// <summary>
        /// Event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="keyWrapper"></param>
        private void KeyWrapper_Updated(object? sender, KeyWrapper keyWrapper)
        {
            if (keyWrapper is null) return;

            // Only update the key bitmap if the key is currently showing
            // This optimizes resource usage by keys that are not currently showing
            // We call CreateKeyBitmap on demand when navigating to a new page instead
            if (keyWrapper.IsShowing)
            {
                keyWrapper.CachedComposedKeyBitmapImage = CreateKeyBitmap(keyWrapper);
            }

            // It's intended to check IsShowing again here
            // The key might not be showing anymore if a page navigation happened while
            // waiting for creation of the new key bitmap, in rare occations that might
            // lead to setting the key bitmap while on the wrong page
            // TODO: Fix with locking?
            if (keyWrapper.IsShowing)
            {
                _streamDeck?.SetKeyBitmap(keyWrapper.Key.Index, keyWrapper.CachedComposedKeyBitmapImage);
            }
        }

        public void ClearKeys()
        {
            _streamDeck?.ClearKeys();
        }

        public KeyBitmap CreateKeyBitmap(KeyWrapper keyWrapper)
        {
            lock (keyWrapper.RenderLock)
            {
                // The composed tile bitmap is only an intermediate: KeyBitmap copies the
                // pixel data out of it. Disposing returns the buffer to ImageSharp's pool
                // instead of leaving it for the finalizer.
                using var tileBitmap = CreateTileBitmap(keyWrapper);
                return KeyBitmap.Create.FromImageSharpImage(tileBitmap);
            }
        }

        /// <summary>
        /// Fonts are immutable and reusable, but building one parses font metrics.
        /// Caching avoids doing that on every key redraw.
        /// </summary>
        private Font GetFont(FontFamily fontFamily, float fontSize, FontStyle fontStyle)
            => _fontCache.GetOrAdd(
                (fontFamily.Name, fontSize, fontStyle),
                key => fontFamily.CreateFont(key.Item2, key.Item3));

        private Image CreateTileBitmap(KeyWrapper keyWrapper)
        {
            // TODO: This method is a mess and should be massively optimized
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
            Image? image = keyWrapper.Image ?? (keyWrapper.Key.DisableTileImage ? null : keyWrapper.Tile?.Image);
            int imagePadding = keyWrapper.Key.ImagePadding ?? keyWrapper.Tile?.ImagePadding ?? 0;
            bool? indicator = keyWrapper.Tile?.ShowIndicator;
            Color indicatorColor = keyWrapper.Key.ActivityIndicatorColorAsColor ?? keyWrapper.Tile?.IndicatorColor ?? Color.Yellow;
            bool? folderIndicator = keyWrapper.Key.ShowFolderIndicator ?? (keyWrapper.Key.Profile != null && keyWrapper.Key.Page != null);
            Color folderIndicatorColor = keyWrapper.Key.FolderIndicatorColorAsColor ?? Color.Blue;
            int textOffsetX = keyWrapper.Key.TextOffsetX ?? 0;
            int textOffsetY = keyWrapper.Key.TextOffsetY ?? 0;
            int imageOffsetX = keyWrapper.Key.ImageOffsetX ?? 0;
            int imageOffsetY = keyWrapper.Key.ImageOffsetY ?? 0;

            // Create key bitmap
            var i = new Image<Rgba32>(_streamDeck.Keys.KeySize, _streamDeck.Keys.KeySize);

            // Add background color
            i.Mutate(x => x.Clear(bgColor));

            // Add image
            if (image != null)
            {
                var targetSize = _streamDeck.Keys.KeySize - (imagePadding * 2);

                // Previously this resized the caller's image in place on every draw,
                // which re-ran the resampler needlessly and corrupted the plugin's
                // own Image instance. The scaled result is cached per key instead.
                var scaled = keyWrapper.GetScaledImage(image, targetSize);
                i.Mutate(x => x.DrawImage(scaled, new Point(imagePadding + imageOffsetX, imagePadding + imageOffsetY), 1f));
            }

            // Add text
            if (text != null && !string.IsNullOrWhiteSpace(text))
            {
                if (_fontCollection.TryGet(font, out var fontFamily))
                {
                    var fontStyle = FontStyle.Regular;
                    if (fontBold) fontStyle |= FontStyle.Bold;
                    if (fontItalic) fontStyle |= FontStyle.Italic;

                    var f = GetFont(fontFamily, fontSize, fontStyle);

                    var center = new PointF(_streamDeck.Keys.KeySize / 2, _streamDeck.Keys.KeySize / 2);
                    var textOptions = new RichTextOptions(f)
                    {
                        Origin = new PointF(center.X + textOffsetX, center.Y + textOffsetY),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        LineSpacing = lineSpacing
                    };

                    if (_configurationProvider.LoadedConfiguration.FallbackFont is not null &&
                        _fontCollection.TryGet(_configurationProvider.LoadedConfiguration.FallbackFont, out var fallBackFontFamily))
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
