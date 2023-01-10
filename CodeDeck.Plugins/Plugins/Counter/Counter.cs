using CodeDeck.PluginAbstractions;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.Counter;

public class Counter : CodeDeckPlugin
{
    private static int _cnt = 0;

    public class CounterTile : Tile
    {
        public override async Task Init()
        {
            Text = $"Counter\n{_cnt}";

            await Task.CompletedTask;
        }

        public override async Task OnTilePressDown()
        {
            _cnt++;
            Text = $"Counter\n{_cnt}";

            await Task.CompletedTask;
        }
    }
}
