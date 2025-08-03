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
        public class TradfriBlindTile : Tile
        {
            [Setting] public string? Blinds { get; set; }
            [Setting] public int? Position { get; set; } = null;

            private List<string>? _devices;

            public override async Task Init(CancellationToken cancellationToken)
            {
                var defaultImageName = "blind01";
                if (Position == 0) defaultImageName = "blind01";
                if (Position > 0) defaultImageName = "blind02";
                if (Position > 33) defaultImageName = "blind03";
                if (Position > 66) defaultImageName = "blind04";
                if (Position == 100) defaultImageName = "blind05";

                var defaultImagePath = Path.GetFullPath($"Plugins/IkeaTradfri/Images/{defaultImageName}.png");
                if (File.Exists(defaultImagePath)) Image = SixLabors.ImageSharp.Image.Load(defaultImagePath);

                if (Blinds != null)
                {
                    _devices = Blinds.Split(',', ';').Select(x => x.Trim()).ToList();
                }
            }

            public override async Task OnTilePressUp(CancellationToken cancellationToken)
            {
                if (_devices is null || !_devices.Any() || Position is null) return;

                await SetBlinds(_devices, Position.Value, cancellationToken);
            }
        }
    }
}
