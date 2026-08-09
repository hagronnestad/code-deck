using CodeDeck.PluginAbstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.IkeaTradfri
{
    public partial class IkeaTradfri : CodeDeckPlugin
    {
        public class TradfriOutletTile : Tile
        {
            [Setting] public string? Outlets { get; set; }
            [Setting] public bool? On { get; set; } = null;

            private List<string>? _devices;

            public override async Task Init(CancellationToken cancellationToken)
            {
                Text = "Tradfri\nOutlet";

                if (Outlets != null)
                {
                    _devices = Outlets.Split(',', ';').Select(x => x.Trim()).ToList();
                }
            }

            public override async Task OnTilePressUp(CancellationToken cancellationToken)
            {
                if (_devices is null || !_devices.Any()) return;

                await SetOutlets(_devices, On, cancellationToken);
            }
        }
    }
}
