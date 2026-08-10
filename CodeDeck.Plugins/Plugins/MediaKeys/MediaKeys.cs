using System.Runtime.InteropServices;

namespace CodeDeck.Plugins.Plugins.Runner
{
    public partial class MediaKeys : CodeDeckPlugin
    {
        public const int KEYEVENTF_EXTENTEDKEY = 1;
        public const int KEYEVENTF_KEYUP = 0;
        public const int VK_VOLUME_MUTE = 0xAD;
        public const int VK_VOLUME_DOWN = 0xAE;
        public const int VK_VOLUME_UP = 0xAF;
        public const int VK_MEDIA_NEXT_TRACK = 0xB0;
        public const int VK_MEDIA_PREV_TRACK = 0xB1;
        public const int VK_MEDIA_STOP = 0xB2;
        public const int VK_MEDIA_PLAY_PAUSE = 0xB3;

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, IntPtr extraInfo);

        public class MuteTile : Tile
        {
            public override Task OnTilePressUp(CancellationToken cancellationToken)
            {
                keybd_event(VK_VOLUME_MUTE, 0, KEYEVENTF_EXTENTEDKEY, IntPtr.Zero);
                return Task.CompletedTask;
            }
        }

        public class VolumeDownTile : Tile
        {
            private CancellationTokenSource? _whileDownCts;

            public override async Task OnTilePressDown(CancellationToken cancellationToken)
            {
                _whileDownCts?.Cancel();
                _whileDownCts = new();
                RunEvery(
                    "Volume down repeat",
                    TimeSpan.FromMilliseconds(75),
                    _ =>
                    {
                        keybd_event(VK_VOLUME_DOWN, 0, KEYEVENTF_EXTENTEDKEY, IntPtr.Zero);
                        return Task.CompletedTask;
                    },
                    cancellationToken: _whileDownCts.Token);
                await Task.CompletedTask;
            }

            public override Task OnTilePressUp(CancellationToken cancellationToken)
            {
                _whileDownCts?.Cancel();
                return Task.CompletedTask;
            }

            public override Task DeInit()
            {
                _whileDownCts?.Cancel();
                return Task.CompletedTask;
            }
        }

        public class VolumeUpTile : Tile
        {
            private CancellationTokenSource? _whileDownCts;

            public override async Task OnTilePressDown(CancellationToken cancellationToken)
            {
                _whileDownCts?.Cancel();
                _whileDownCts = new();
                RunEvery(
                    "Volume up repeat",
                    TimeSpan.FromMilliseconds(75),
                    _ =>
                    {
                        keybd_event(VK_VOLUME_UP, 0, KEYEVENTF_EXTENTEDKEY, IntPtr.Zero);
                        return Task.CompletedTask;
                    },
                    cancellationToken: _whileDownCts.Token);
                await Task.CompletedTask;
            }

            public override Task OnTilePressUp(CancellationToken cancellationToken)
            {
                _whileDownCts?.Cancel();
                return Task.CompletedTask;
            }

            public override Task DeInit()
            {
                _whileDownCts?.Cancel();
                return Task.CompletedTask;
            }
        }

        public class NextTrackTile : Tile
        {
            public override Task OnTilePressUp(CancellationToken cancellationToken)
            {
                keybd_event(VK_MEDIA_NEXT_TRACK, 0, KEYEVENTF_EXTENTEDKEY, IntPtr.Zero);
                return Task.CompletedTask;
            }
        }

        public class PreviousTrackTile : Tile
        {
            public override Task OnTilePressUp(CancellationToken cancellationToken)
            {
                keybd_event(VK_MEDIA_PREV_TRACK, 0, KEYEVENTF_EXTENTEDKEY, IntPtr.Zero);
                return Task.CompletedTask;
            }
        }

        public class StopTile : Tile
        {
            public override Task OnTilePressUp(CancellationToken cancellationToken)
            {
                keybd_event(VK_MEDIA_STOP, 0, KEYEVENTF_EXTENTEDKEY, IntPtr.Zero);
                return Task.CompletedTask;
            }
        }

        public class PlayPauseTile : Tile
        {
            public override Task OnTilePressUp(CancellationToken cancellationToken)
            {
                keybd_event(VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_EXTENTEDKEY, IntPtr.Zero);
                return Task.CompletedTask;
            }
        }

    }
}
