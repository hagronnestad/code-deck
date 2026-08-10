using NissanConnectLib.Api;
using NissanConnectLib.Enums;
using NissanConnectLib.Models;
using System.Diagnostics;
using System.Text.Json;

namespace CodeDeck.Plugins.Plugins.Runner
{
    public class NissanConnect : CodeDeckPlugin
    {
        [Setting] public string? Email { get; set; }
        [Setting] public string? Password { get; set; }

        private static string _tokenCacheFile = string.Empty;

        private static NissanConnectClient _client = new();
        private static string? _userId = null;


        public override Task InitializeAsync(PluginContext context, CancellationToken cancellationToken)
        {
            _tokenCacheFile = Path.Combine(context.PluginPath, "token.cache");
            RunTask("Nissan Connect initialization", token => InitAsync(Email, Password, token));
            return Task.CompletedTask;
        }

        private static async Task InitAsync(string? email, string? password, CancellationToken cancellationToken)
        {
            // Instantiate client
            _client = new NissanConnectClient(NissanConnectLib.Configuration.Region.EU);

            // Save token to cache file when refreshed
            _client.AccessTokenRefreshed += (sender, token) =>
            {
                Debug.WriteLine("Access token refreshed!");
                File.WriteAllText(_tokenCacheFile, JsonSerializer.Serialize(_client.AccessToken));
            };

            var loggedIn = false;

            // Try to use token cache file
            if (File.Exists(_tokenCacheFile))
            {
                var cachedToken = JsonSerializer.Deserialize<OAuthAccessTokenResult>(File.ReadAllText(_tokenCacheFile));
                _client.AccessToken = cachedToken;

                if (await _client.GetUserId() is null)
                {
                    Debug.WriteLine("Could not get user ID using cached token, deleting cache file...");
                    File.Delete(_tokenCacheFile);
                }
                else
                {
                    Debug.WriteLine("Cached token is valid!");
                    loggedIn = true;
                }
            }

            // Log in using username and password
            if (!loggedIn)
            {
                // Log in using a username and password
                if (email is not null && password is not null)
                {
                    loggedIn = await _client.LogIn(email, password);
                    if (loggedIn)
                    {
                        Debug.WriteLine("Logged in using username and password. Writing token to cache file...");
                        File.WriteAllText(_tokenCacheFile, JsonSerializer.Serialize(_client.AccessToken));
                    }
                    else
                    {
                        Debug.WriteLine("Login failed!");
                        return;
                    }
                }
            }

            // Get the user id
            _userId = await _client.GetUserId();
            if (_userId == null)
            {
                Debug.WriteLine("Couldn't get user!");
                return;
            }
            Debug.WriteLine($"Logged in as: {_userId}");
        }

        private static async Task<Car?> GetCar(string? nickName, CancellationToken cancellationToken)
        {
            // TODO: Fix this hack to prevent first tile Update calls to fail
            // Wait until logged in
            while (_userId is null)
            {
                await Task.Delay(1000, cancellationToken);
            }

            var cars = await _client.GetCars(_userId);

            var car = nickName is null ?
                (cars?.FirstOrDefault()) :
                (cars?.FirstOrDefault(c => c.VehicleNickName?.ToLower() == nickName.ToLower()));

            return car;
        }

        public class NissanConnectBatteryLevelTile : Tile
        {
            [Setting] public int Interval { get; set; } = 10; // minutes
            [Setting] public string? NickName { get; set; }
            [Setting] public bool ForceRefreshAuto { get; set; } = false;
            [Setting] public bool ForceRefreshTilePress { get; set; } = false;
            [Setting] public string Format { get; set; } = "{0}";
            [Setting] public string FormatError { get; set; } = "🚗❌";

            private Car? _car;

            public override async Task Init(CancellationToken cancellationToken)
            {
                await base.Init(cancellationToken);
                Text = "Nissan\nConnect\nBattery\nLevel";
                RunEvery("Battery level update", TimeSpan.FromMinutes(Interval), token => Update(ForceRefreshAuto, token));
            }

            public override async Task OnTilePressDown(CancellationToken cancellationToken)
            {
                await Update(ForceRefreshTilePress, cancellationToken);
                await base.OnTilePressDown(cancellationToken);
            }

            private async Task Update(bool forceRefresh, CancellationToken cancellationToken)
            {
                try
                {
                    ShowIndicator = true;

                    _car = await GetCar(NickName, cancellationToken);

                    if (_car is not null && _car.Vin is not null)
                    {
                        var bs = await _client.GetBatteryStatus(_car.Vin, forceRefresh, TimeSpan.FromSeconds(60));

                        if (bs is null)
                        {
                            Text = FormatError;
                        }
                        else
                        {
                            Text = string.Format(Format, bs.BatteryLevel, bs.ChargeStatus,
                                $"{bs.BatteryStatusAge?.TotalMinutes:N0}");
                        }
                    }
                }
                catch (Exception)
                {
                    Text = FormatError;
                }
                finally
                {
                    ShowIndicator = false;
                }
            }

        }

        public class NissanConnectRangeTile : Tile
        {
            [Setting] public int Interval { get; set; } = 10; // minutes
            [Setting] public string? NickName { get; set; }
            [Setting] public bool ForceRefreshAuto { get; set; } = false;
            [Setting] public bool ForceRefreshTilePress { get; set; } = false;
            [Setting] public string Format { get; set; } = "🏁\n{0} km\n({1} km)";
            [Setting] public string FormatError { get; set; } = "🚗❌";

            private Car? _car;

            public override async Task Init(CancellationToken cancellationToken)
            {
                await base.Init(cancellationToken);
                Text = "Nissan\nConnect\nBattery\nLevel";
                RunEvery("Range update", TimeSpan.FromMinutes(Interval), token => Update(ForceRefreshAuto, token));
            }

            public override async Task OnTilePressDown(CancellationToken cancellationToken)
            {
                await Update(ForceRefreshTilePress, cancellationToken);
                await base.OnTilePressDown(cancellationToken);
            }

            private async Task Update(bool forceRefresh, CancellationToken cancellationToken)
            {
                try
                {
                    ShowIndicator = true;

                    _car = await GetCar(NickName, cancellationToken);

                    if (_car is not null && _car.Vin is not null)
                    {
                        var bs = await _client.GetBatteryStatus(_car.Vin, forceRefresh, TimeSpan.FromSeconds(60));

                        if (bs is null)
                        {
                            Text = FormatError;
                        }
                        else
                        {
                            Text = string.Format(Format, bs.RangeHvacOn, bs.RangeHvacOff);
                        }
                    }
                }
                catch (Exception)
                {
                    Text = FormatError;
                }
                finally
                {
                    ShowIndicator = false;
                }
            }

        }

        public class NissanConnectBatteryStatusAge : Tile
        {
            [Setting] public int Interval { get; set; } = 10; // minutes
            [Setting] public string? NickName { get; set; }
            [Setting] public bool ForceRefreshAuto { get; set; } = false;
            [Setting] public bool ForceRefreshTilePress { get; set; } = false;
            [Setting] public string Format { get; set; } = "{0}\n{1}";
            [Setting] public string FormatAge { get; set; } = "HH\\:mm";
            [Setting] public string FormatDateTime { get; set; } = "HH\\:mm";
            [Setting] public string FormatError { get; set; } = "🚗❌";

            private Car? _car;

            public override async Task Init(CancellationToken cancellationToken)
            {
                Text = "Nissan\nConnect\nBattery\nStatus";
                _car = await GetCar(NickName, cancellationToken);
                RunEvery("Battery status age update", TimeSpan.FromMinutes(Interval), token => Update(ForceRefreshAuto, token));
                await base.Init(cancellationToken);
            }

            public override async Task OnTilePressDown(CancellationToken cancellationToken)
            {
                await Update(ForceRefreshTilePress, cancellationToken);
                await base.OnTilePressDown(cancellationToken);
            }

            private async Task Update(bool forceRefresh, CancellationToken cancellationToken)
            {
                try
                {
                    ShowIndicator = true;

                    if (_car is not null && _car.Vin is not null)
                    {
                        var bs = await _client.GetBatteryStatus(_car.Vin);

                        if (bs is null)
                        {
                            Text = FormatError;
                        }
                        else
                        {
                            var batteryStatusAge = bs.BatteryStatusAge?.ToString(FormatAge);
                            var updateDateTime = bs.LastUpdateTime?.ToLocalTime().ToString(FormatDateTime);
                            Text = string.Format(Format, batteryStatusAge, updateDateTime);
                        }
                    }
                }
                catch (Exception)
                {
                    Text = FormatError;
                }
                finally
                {
                    ShowIndicator = false;
                }
            }

        }

        public class NissanConnectChargeStatus : Tile
        {
            [Setting] public int Interval { get; set; } = 10; // minutes
            [Setting] public string? NickName { get; set; }
            [Setting] public bool ForceRefreshAuto { get; set; } = false;
            [Setting] public bool ForceRefreshTilePress { get; set; } = false;
            [Setting] public string FormatNotCharging { get; set; } = "🔋";
            [Setting] public string FormatCharging { get; set; } = "⚡";
            [Setting] public string FormatPluggedIn { get; set; } = "🔌";
            [Setting] public string FormatError { get; set; } = "🚗❌";

            private Car? _car;

            public override async Task Init(CancellationToken cancellationToken)
            {
                Text = "Nissan\nConnect\nCharge\nStatus";
                _car = await GetCar(NickName, cancellationToken);
                RunEvery("Charge status update", TimeSpan.FromMinutes(Interval), token => Update(ForceRefreshAuto, token));
                await base.Init(cancellationToken);
            }

            public override async Task OnTilePressDown(CancellationToken cancellationToken)
            {
                await Update(ForceRefreshTilePress, cancellationToken);
                await base.OnTilePressDown(cancellationToken);
            }

            private async Task Update(bool forceRefresh, CancellationToken cancellationToken)
            {
                try
                {
                    ShowIndicator = true;

                    if (_car is not null && _car.Vin is not null)
                    {
                        var bs = await _client.GetBatteryStatus(_car.Vin);

                        if (bs is null)
                        {
                            Text = FormatError;
                        }
                        else
                        {
                            if (bs.ChargeStatus == ChargeStatus.NotCharging)
                            {
                                Text = FormatNotCharging;
                            }
                            if (bs.PlugStatus == PlugStatus.PluggedIn)
                            {
                                Text = FormatPluggedIn;
                            }
                            if (bs.ChargeStatus == ChargeStatus.Charging)
                            {
                                Text = FormatCharging;
                            }
                            if (bs.ChargeStatus == ChargeStatus.Error || bs.PlugStatus == PlugStatus.Error)
                            {
                                Text = FormatError;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    Text = FormatError;
                }
                finally
                {
                    ShowIndicator = false;
                }
            }

        }
    }
}
