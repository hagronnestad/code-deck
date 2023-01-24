using CodeDeck.PluginAbstractions;
using InputSimulatorEx;
using InputSimulatorEx.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.Counter;

public class KeyboardSimulator : CodeDeckPlugin
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

    public class HotkeyTile : Tile
    {
        [Setting] public string? Modifiers { get; set; }
        [Setting] public string? Keys { get; set; }

        private List<VirtualKeyCode>? _modifiers;
        private List<VirtualKeyCode>? _keys;

        private InputSimulator _inputSimulator = new();

        public override Task Init(CancellationToken cancellationToken)
        {
            try
            {
                _modifiers = Modifiers?
                    .Split(" ")
                    .Select(x => (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), x, true))
                    .ToList();

                _keys = Keys?
                    .Split(" ")
                    .Select(x => (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), x, true))
                    .ToList();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception in {nameof(HotkeyTile)}. Message: {e.Message}");
            }

            return base.Init(cancellationToken);
        }

        public override Task OnTilePressDown(CancellationToken cancellationToken)
        {
            if (_modifiers is not null && _keys is not null)
            {
                _inputSimulator.Keyboard.ModifiedKeyStroke(_modifiers, _keys);
            }
            else if (_keys is not null && _keys.Count > 0)
            {
                _inputSimulator.Keyboard.KeyPress(_keys.First());
            }
            
            return base.OnTilePressDown(cancellationToken);
        }
    }
}
