using CodeDeck.Models.Configuration;
using CodeDeck.PluginAbstractions;
using CodeDeck.PluginSystem;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CodeDeck
{
    public class KeyWrapper
    {
        private readonly ILogger _logger;

        public Profile Profile { get; set; }
        public Page Page { get; set; }
        public Key Key { get; set; }

        public LoadedPlugin? Plugin { get; set; }
        public Tile? Tile { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; } = new();

        public Image? Image { get; set; }

        public event EventHandler<KeyWrapper>? Updated;

        public KeyWrapper(ILogger logger, Profile profile, Page page, Key key, LoadedPlugin? plugin)
        {
            _logger = logger;

            Profile = profile;
            Page = page;
            Key = key;
            Plugin = plugin;
        }

        public async Task InstantiateTileObjectAsync()
        {
            if (Plugin is null || Key.Tile is null)
            {
                return;
            }

            Tile = Plugin.CreateTileInstance(Key.Tile, Key.Settings);

            if (Tile is null)
            {
                return;
            }

            Tile.NotifyChange = NotifyChange_Action;
            Tile.Text = Key.Text;
            Tile.Font = Key.Font;
            Tile.FontSize = Key.FontSize;
            Tile.ImagePadding = Key.ImagePadding;

            try
            {
                await Tile.Init(CancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception during 'Tile.Init' in plugin: {Plugin.Name}; tile: {Tile.GetType().Name}. Message: '{e.Message}'");
            }
        }

        public void NotifyChange_Action()
        {
            Updated?.Invoke(this, this);
        }

        public async void HandleKeyPressDown()
        {
            if (Tile == null) return;

            try
            {
                await Tile.OnTilePressDown(CancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Tile.OnTilePressDown(): {e.Message}");
            }
        }

        public async void HandleKeyPressUp()
        {
            if (Tile == null) return;

            try
            {
                await Tile.OnTilePressUp(CancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Tile.OnTilePressUp(): {e.Message}");
            }
        }
    }
}
