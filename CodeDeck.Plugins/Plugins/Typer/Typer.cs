using CodeDeck.PluginAbstractions;
using InputSimulatorEx;
using System.Threading;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.Counter;

public class Typer : CodeDeckPlugin
{
    public class TyperTile : Tile
    {
        private string? _text;
        private InputSimulator _inputSimulator = new();

        public override Task Init(CancellationToken cancellationToken)
        {
            Settings?.TryGetValue("text", out _text);

            return base.Init(cancellationToken);
        }

        public override async Task OnTilePressDown(CancellationToken cancellationToken)
        {
            _inputSimulator.Keyboard.TextEntry(_text ?? "No text specified!");

            await Task.CompletedTask;
        }
    }
}
