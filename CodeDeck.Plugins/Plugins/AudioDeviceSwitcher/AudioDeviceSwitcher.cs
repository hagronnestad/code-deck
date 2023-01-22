using AudioSwitcher.AudioApi.CoreAudio;
using CodeDeck.PluginAbstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.AudioDeviceSwitcher
{
    [SupportedOSPlatform("windows")]
    public class AudioDeviceSwitcher : CodeDeckPlugin
    {
        private static CoreAudioController? _audioController;
        private static IEnumerable<CoreAudioDevice>? _devices;

        static AudioDeviceSwitcher()
        {
            _audioController = new CoreAudioController();
            _devices = _audioController.GetPlaybackDevices();
        }

        public class AudioDeviceSwitcherTile : Tile
        {
            [Setting] public string? Device { get; set; }

            private CoreAudioDevice? _device = null;

            public override async Task Init(CancellationToken cancellationToken)
            {
                if (Device == null) return;
                if (_devices == null) throw new Exception("No devices found!");

                _device = _devices.FirstOrDefault(x => x.FullName == Device);
                if (_device == null) throw new Exception($"Device; '{Device}' not found.");

                await Task.CompletedTask;
            }

            public override async Task OnTilePressDown(CancellationToken cancellationToken)
            {
                if (_device == null) return;
                _audioController?.SetDefaultDevice(_device);

                SystemSounds.Beep.Play();

                await Task.CompletedTask;
            }
        }
    }
}
