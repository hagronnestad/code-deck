using CodeDeck.Models.Configuration;
using CodeDeck.PluginAbstractions;
using CodeDeck.PluginSystem;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using System;
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

        public Plugin? Plugin { get; set; }
        public Tile? Tile { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; } = new();

        public Image? Image { get; set; }

        public event EventHandler<KeyWrapper>? Updated;

        public KeyWrapper(ILogger logger, Profile profile, Page page, Key key, Plugin? plugin)
        {
            _logger = logger;

            Profile = profile;
            Page = page;
            Key = key;
            Plugin = plugin;
        }

        public async Task InstantiateTileObjectAsync()
        {
            if (Plugin is null || Key.Tile is null) return;

            try
            {
                Tile = Plugin.CreateTileInstance(Key.Tile, Key.Settings);
                if (Tile is null) return;

                Tile.NotifyChange = NotifyChange_Action;

                await Tile.Init(CancellationTokenSource.Token);
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

            try
            {
                await Tile.OnTilePressDown(CancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                _logger.LogDebug($"<{nameof(Tile.OnTilePressDown)}>: {e.Message}");
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
                _logger.LogDebug($"<{nameof(Tile.OnTilePressUp)}>: {e.Message}");
            }
        }
    }
}
