using CodeDeck.PluginAbstractions;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.Counter;

public class Lock : CodeDeckPlugin
{
    public class LockTile : Tile
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool LockWorkStation();

        public override Task Init(CancellationToken cancellationToken)
        {
            Text = "Lock\nPC";
            return base.Init(cancellationToken);
        }

        public override Task OnTilePressUp(CancellationToken cancellationToken)
        {
            LockWorkStation();
            return base.OnTilePressDown(cancellationToken);
        }
    }
}
