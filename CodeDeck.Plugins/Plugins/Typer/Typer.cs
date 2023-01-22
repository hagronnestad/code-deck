using CodeDeck.PluginAbstractions;
using InputSimulatorEx;
using System.Threading;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.Counter;

public class Typer : CodeDeckPlugin
{
    public class TyperTile : Tile
    {
        [Setting] public string? Text { get; set; }

        private InputSimulator _inputSimulator = new();

        public override Task Init(CancellationToken cancellationToken)
        {
            return base.Init(cancellationToken);
        }

        public override async Task OnTilePressDown(CancellationToken cancellationToken)
        {
            _inputSimulator.Keyboard.TextEntry(Text ?? "No text specified!");
            await Task.CompletedTask;
        }
    }
}
