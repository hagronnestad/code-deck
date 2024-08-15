using CodeDeck.PluginAbstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tomidix.NetStandard.Tradfri;
using Tomidix.NetStandard.Tradfri.Models;
using Zeroconf;

namespace CodeDeck.Plugins.Plugins.IkeaTradfri
{
    public partial class IkeaTradfri : CodeDeckPlugin
    {
        [Setting] public static string? AppName { get; set; } = "CodeDeck";
        [Setting] public static string? AppKey { get; set; }

        private const string COLOR_TEMP_1_COOL_WHITE = "f5faf6";
        private const string COLOR_TEMP_2_WARM_WHITE = "f2eccf";
        private const string COLOR_TEMP_3_INCADESCENT = "f1e0b5";
        private const string COLOR_TEMP_4_CANDLELIGHT = "efd275";

        private static TradfriController? _controller = null;

        static IkeaTradfri()
        {
            _ = InitTradfri();
        }

        private static async Task<bool> InitTradfri()
        {
            var ip = await DiscoverTradfriGateway();
            if (ip is null) return false;

            _controller = new TradfriController(AppName, ip);
            _controller.ConnectAppKey(AppKey, AppName);

            var gwinfo = await _controller.GatewayController.GetGatewayInfo();
            var time = gwinfo.CurrentTimeUnix;

            return true;
        }

        /// <summary>
        /// Turn a Tradfri Light ON/OFF
        /// If state is null, the light state will be toggled
        /// </summary>
        /// <param name="names"></param>
        /// <param name="state"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task SetLights(List<string> names, CancellationToken cancellationToken, bool? state, int? dimmer, string? color)
        {
            if (_controller is null || _controller.GatewayController is null) return;

            var allDevices = await _controller.GatewayController.GetDeviceObjects();

            var devices = allDevices
                .Where(x => x.DeviceType == DeviceType.Light)
                .Where(x => names.Contains(x.Name));

            await Parallel.ForEachAsync(devices, cancellationToken, async (device, cancellationToken) =>
            {
                // If state is null, we want to toggle the current state
                if (state is not null)
                {
                    await _controller.DeviceController.SetLight(device, state.Value);
                }

                if (dimmer is not null)
                {
                    var value = 254 / 100.0f * dimmer.Value;
                    await _controller.DeviceController.SetDimmer(device, (int)value);
                }

                if (color is not null && !string.IsNullOrWhiteSpace(color))
                {
                    await _controller.DeviceController.SetColor(device, color);
                }
            });
        }

        /// <summary>
        /// Turn a Tradfri Outlet ON/OFF
        /// If state is null, the outlet state will be toggled
        /// </summary>
        /// <param name="names"></param>
        /// <param name="state"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task SetOutlets(List<string> names, bool? state, CancellationToken cancellationToken)
        {
            if (_controller is null || _controller.GatewayController is null) return;

            var allDevices = await _controller.GatewayController.GetDeviceObjects();

            var devices = allDevices
                .Where(x => x.DeviceType == DeviceType.ControlOutlet)
                .Where(x => names.Contains(x.Name));

            await Parallel.ForEachAsync(devices, cancellationToken, async (device, cancellationToken) =>
            {
                if (state is null)
                {
                    var newState = device.Control.First().State == Bool.False ? true : false;
                    await _controller.DeviceController.SetOutlet(device, newState);
                }
                else
                {
                    await _controller.DeviceController.SetOutlet(device, state.Value);
                }
            });
        }

        private static async Task SetBlinds(List<string> names, int position, CancellationToken cancellationToken)
        {
            if (_controller is null || _controller.GatewayController is null) return;

            var allDevices = await _controller.GatewayController.GetDeviceObjects();

            var devices = allDevices
                .Where(x => x.DeviceType == DeviceType.Blind)
                .Where(x => names.Contains(x.Name));

            await Parallel.ForEachAsync(devices, cancellationToken, async (device, cancellationToken) =>
            {
                await _controller.DeviceController.SetBlind(device, position);
            });
        }

        private static async Task<string?> DiscoverTradfriGateway()
        {
            var results = await ZeroconfResolver.ResolveAsync("_coap._udp.local.", TimeSpan.FromMilliseconds(5000));
            if (!results.Any()) return null;

            var ip = results.FirstOrDefault(x => x.DisplayName.StartsWith("gw-"))?.IPAddress;
            if (ip == null || string.IsNullOrWhiteSpace(ip)) return null;

            return ip;
        }

        private static string? GetColorCode(string? color)
        {
            if (color is null) return null;

            switch (color.ToLower())
            {
                case "cool":
                    return COLOR_TEMP_1_COOL_WHITE;

                case "warm":
                    return COLOR_TEMP_2_WARM_WHITE;

                case "incadescent":
                    return COLOR_TEMP_3_INCADESCENT;

                case "candle":
                    return COLOR_TEMP_4_CANDLELIGHT;

                default:
                    return null;
            }
        }
    }
}
