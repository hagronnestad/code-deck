using CodeDeck.PluginAbstractions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.IkeaTradfri
{
    public partial class IkeaTradfri : CodeDeckPlugin
    {
        public class TradfriLightTile : Tile
        {
            [Setting] public string? Lights { get; set; }
            [Setting] public bool On { get; set; } = true;
            [Setting] public int? Brightness { get; set; }
            [Setting] public string? Color { get; set; }

            private List<string>? _devices;

            public override async Task Init(CancellationToken cancellationToken)
            {
                var defaultImage = Path.GetFullPath($"Plugins/IkeaTradfri/Images/bulb03.png");
                if (File.Exists(defaultImage)) Image = SixLabors.ImageSharp.Image.Load(defaultImage);

                if (Lights != null)
                {
                    _devices = Lights.Split(',', ';').Select(x => x.Trim()).ToList();
                }
            }

            public override async Task OnTilePressUp(CancellationToken cancellationToken)
            {
                if (_devices is null || !_devices.Any()) return;

                await SetLights(_devices, cancellationToken, On, Brightness, GetColorCode(Color));
            }
        }
    }
}
