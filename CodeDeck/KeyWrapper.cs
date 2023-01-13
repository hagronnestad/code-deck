using CodeDeck.Models.Configuration;
using CodeDeck.PluginAbstractions;
using CodeDeck.PluginSystem;
using SixLabors.ImageSharp;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CodeDeck
{
    public class KeyWrapper
    {
        public Profile Profile { get; set; }
        public Page Page { get; set; }
        public Key Key { get; set; }

        public LoadedPlugin? Plugin { get; set; }
        public Tile? Tile { get; set; }

        public Image? Image { get; set; }

        public event EventHandler<KeyWrapper>? Updated;

        public KeyWrapper(Profile profile, Page page, Key key, LoadedPlugin? plugin)
        {
            Profile = profile;
            Page = page;
            Key = key;
            Plugin = plugin;
        }

        public async Task InstantiateTileObjectAsync()
        {
            if (Plugin?.Instance == null)
            {
                return;
            }

            var tileType = Plugin?.Instance
                .GetType()
                .GetNestedTypes()
                .Where(x => x.BaseType == typeof(Tile))
                .Where(x => x.Name == Key.Tile)
                .FirstOrDefault();

            if (tileType != null)
            {
                Tile = Activator.CreateInstance(tileType) as Tile;
                
                if (Tile != null)
                {
                    Tile.NotifyChange = NotifyChange_Action;
                    Tile.Text = Key.Text;
                    Tile.Font = Key.Font;
                    Tile.FontSize = Key.FontSize;
                    Tile.Settings = Key.Settings;
                    Tile.ImagePadding = Key.ImagePadding;

                    try
                    {
                        await Tile.Init();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Tile.Init(): {e.Message}");
                    }
                }
            }
        }

        public void NotifyChange_Action()
        {
            Updated?.Invoke(this, this);
        }

        public async void HandleKeyPressDown()
        {
            if (Plugin?.Instance == null || Tile == null) return;

            try
            {
                await Tile.OnTilePressDown();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Tile.OnTilePressDown(): {e.Message}");
            }
        }

        public async void HandleKeyPressUp()
        {
            if (Plugin?.Instance == null || Tile == null) return;

            try
            {
                await Tile.OnTilePressUp();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Tile.OnTilePressUp(): {e.Message}");
            }
        }
    }
}
