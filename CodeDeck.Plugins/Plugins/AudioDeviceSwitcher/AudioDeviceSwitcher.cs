using AudioSwitcher.AudioApi.CoreAudio;
using CodeDeck.PluginAbstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.AudioDeviceSwitcher
{
    [SupportedOSPlatform("windows")]
    public class AudioDeviceSwitcher : CodeDeckPlugin
    {
        private static CoreAudioController? _audioController;
        private static IEnumerable<CoreAudioDevice>? _devices;

        public AudioDeviceSwitcher()
        {
            _audioController = new CoreAudioController();
            _devices = _audioController.GetPlaybackDevices();
        }

        public class AudioDeviceSwitcherTile : Tile
        {
            private CoreAudioDevice? _device = null;

            public override async Task Init()
            {
                var deviceName = Settings?["device"];
                if (deviceName == null) return;

                if (_devices == null) throw new Exception("No devices found!");

                _device = _devices.FirstOrDefault(x => x.FullName == deviceName);
                if (_device == null) throw new Exception($"Device; '{deviceName}' not found.");

                await Task.CompletedTask;
            }

            public override async Task OnTilePressDown()
            {
                if (_device == null) return;
                _audioController?.SetDefaultDevice(_device);

                SystemSounds.Beep.Play();

                await Task.CompletedTask;
            }
        }
    }
}
