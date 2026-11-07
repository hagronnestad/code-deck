using NissanConnectLib.Api;
using NissanConnectLib.Enums;
using NissanConnectLib.Models;
using System.Net;
using System.Text.Json;

namespace CodeDeck.Plugins.Plugins.Runner
{
    public class NissanConnect : CodeDeckPlugin
    {
        private const string SessionKey = "NissanConnect.Session";

        [Setting] public string? Email { get; set; }
        [Setting] public string? Password { get; set; }

        public override Task InitializeAsync(PluginContext context, CancellationToken cancellationToken)
        {
            var session = context.GetOrCreateSharedState(
                SessionKey,
                () => new NissanConnectSession(context.PluginPath, Email, Password, Logger));
            RunTask("Nissan Connect initialization", session.InitializeAsync);
            return Task.CompletedTask;
        }

        private static NissanConnectSession GetSession(PluginContext context) =>
            context.GetOrCreateSharedState<NissanConnectSession>(
                SessionKey,
                () => throw new InvalidOperationException("Nissan Connect has not been initialized."));

        private sealed class NissanConnectSession : IDisposable
        {
            private readonly NissanConnectClient _client = new(NissanConnectLib.Configuration.Region.EU);
            private readonly SemaphoreSlim _authenticationLock = new(1, 1);
            private readonly object _tokenCacheLock = new();
            private readonly string _tokenCacheFile;
            private readonly string? _email;
            private readonly string? _password;
            private readonly ILogger _logger;
            private string? _userId;
            private bool _cacheLoaded;
            private volatile bool _hasLoggedIn;

            public NissanConnectSession(string pluginPath, string? email, string? password, ILogger logger)
            {
                _tokenCacheFile = Path.Combine(pluginPath, "token.cache");
                _email = email;
                _password = password;
                _logger = logger;
                _client.AccessTokenRefreshed += (_, token) =>
                {
                    _logger.LogDebug("Access token refreshed.");
                    SaveToken(token);
                };
            }

            private void SaveToken(OAuthAccessTokenResult token)
            {
                lock (_tokenCacheLock)
                {
                    // Do not let an older refresh overwrite a newer login's token.
                    if (ReferenceEquals(token, _client.AccessToken))
                    {
                        File.WriteAllText(_tokenCacheFile, JsonSerializer.Serialize(token));
                    }
                }
            }

            public async Task InitializeAsync(CancellationToken cancellationToken)
            {
                await _authenticationLock.WaitAsync(cancellationToken);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (_userId is not null) return;

                    if (!_cacheLoaded)
                    {
                        _cacheLoaded = true;
                        if (File.Exists(_tokenCacheFile))
                        {
                            _client.AccessToken = JsonSerializer.Deserialize<OAuthAccessTokenResult>(
                                File.ReadAllText(_tokenCacheFile))
                                ?? throw new InvalidOperationException("Nissan Connect token cache is empty.");
                        }
                    }

                    if (_client.AccessToken is not null)
                    {
                        try
                        {
                            _userId = await _client.GetUserId();
                            if (_userId is not null) _logger.LogDebug("Cached authentication is valid.");
                        }
                        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            _logger.LogWarning("Cached authentication is invalid; deleting token cache.");
                            _client.LogOut();
                            File.Delete(_tokenCacheFile);
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    if (_userId is null) await LogInAsync(cancellationToken);
                    _logger.LogInformation("Nissan Connect initialization completed.");
                }
                finally
                {
                    _authenticationLock.Release();
                }
            }

            private async Task LogInAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(_email) || string.IsNullOrEmpty(_password))
                {
                    throw new InvalidOperationException("Nissan Connect requires an email and password to log in.");
                }

                await _client.LogIn(_email, _password);
                _hasLoggedIn = true;
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogInformation("Logged in; writing token cache.");
                SaveToken(_client.AccessToken
                    ?? throw new InvalidOperationException("Nissan Connect login returned no token."));
                _userId = await _client.GetUserId()
                    ?? throw new InvalidOperationException("Nissan Connect returned no user ID.");
                cancellationToken.ThrowIfCancellationRequested();
            }

            private async Task<T> RequestAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
            {
                await InitializeAsync(cancellationToken);
                var hasLoggedIn = _hasLoggedIn;
                T result;
                try
                {
                    result = await operation();
                }
                catch (HttpRequestException exception) when (
                    exception.StatusCode == HttpStatusCode.Unauthorized && !hasLoggedIn)
                {
                    await _authenticationLock.WaitAsync(cancellationToken);
                    try
                    {
                        // The library cannot use configured credentials after a cached-token startup.
                        if (!_hasLoggedIn) await LogInAsync(cancellationToken);
                    }
                    finally
                    {
                        _authenticationLock.Release();
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    result = await operation();
                }

                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }

            public async Task<AttributesBatteryStatus> GetBatteryStatusAsync(
                string? vin, string? nickName, bool forceRefresh, CancellationToken cancellationToken)
            {
                var cars = await RequestAsync(
                    () => _client.GetCars(_userId
                        ?? throw new InvalidOperationException("Nissan Connect has no authenticated user.")),
                    cancellationToken)
                    ?? throw new InvalidOperationException("Nissan Connect returned no vehicle list.");

                Car? car;
                if (!string.IsNullOrWhiteSpace(vin))
                {
                    car = cars.FirstOrDefault(
                        car => string.Equals(car.Vin, vin, StringComparison.OrdinalIgnoreCase));
                }
                else if (!string.IsNullOrWhiteSpace(nickName))
                {
                    car = cars.FirstOrDefault(car =>
                        string.Equals(car.NickName, nickName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(car.VehicleNickName, nickName, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    car = cars.FirstOrDefault();
                }

                if (car is null)
                {
                    throw new InvalidOperationException("Nissan Connect could not find the configured vehicle.");
                }
                if (string.IsNullOrWhiteSpace(car.Vin))
                {
                    throw new InvalidOperationException("Nissan Connect returned a vehicle without a VIN.");
                }

                if (forceRefresh)
                {
                    if (!await RequestAsync(() => _client.RefreshBatteryStatus(car.Vin), cancellationToken))
                    {
                        throw new InvalidOperationException("Nissan Connect rejected the battery refresh request.");
                    }
                    await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
                }

                return await RequestAsync(() => _client.GetBatteryStatus(car.Vin), cancellationToken)
                    ?? throw new InvalidOperationException("Nissan Connect returned no battery status.");
            }

            public void Dispose()
            {
                _client.Dispose();
                _authenticationLock.Dispose();
            }
        }

        public abstract class NissanConnectTile : Tile
        {
            private readonly SemaphoreSlim _updateLock = new(1, 1);

            [Setting] public int Interval { get; set; } = 10; // minutes
            [Setting] public string? Vin { get; set; }
            [Setting] public string? NickName { get; set; }
            [Setting] public bool ForceRefreshAuto { get; set; } = false;
            [Setting] public bool ForceRefreshTilePress { get; set; } = false;
            [Setting] public string FormatError { get; set; } = "🚗❌";

            protected abstract string InitialText { get; }
            protected abstract string UpdateName { get; }
            protected abstract string FormatStatus(AttributesBatteryStatus status);

            public override Task Init(CancellationToken cancellationToken)
            {
                Text = InitialText;
                RunEvery(UpdateName, TimeSpan.FromMinutes(Interval), token => Update(ForceRefreshAuto, token));
                return Task.CompletedTask;
            }

            public override Task OnTilePressDown(CancellationToken cancellationToken) =>
                Update(ForceRefreshTilePress, cancellationToken);

            private async Task Update(bool forceRefresh, CancellationToken cancellationToken)
            {
                await _updateLock.WaitAsync(cancellationToken);
                try
                {
                    ShowIndicator = true;
                    var status = await GetSession(PluginContext)
                        .GetBatteryStatusAsync(Vin, NickName, forceRefresh, cancellationToken);
                    Text = FormatStatus(status);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception)
                {
                    Text = FormatError;
                    throw;
                }
                finally
                {
                    ShowIndicator = false;
                    _updateLock.Release();
                }
            }
        }

        public class NissanConnectBatteryLevelTile : NissanConnectTile
        {
            [Setting] public string Format { get; set; } = "{0}";

            protected override string InitialText => "Nissan\nConnect\nBattery\nLevel";
            protected override string UpdateName => "Battery level update";

            protected override string FormatStatus(AttributesBatteryStatus status) =>
                string.Format(Format, status.BatteryLevel, status.ChargeStatus,
                    $"{status.BatteryStatusAge?.TotalMinutes:N0}");
        }

        public class NissanConnectRangeTile : NissanConnectTile
        {
            [Setting] public string Format { get; set; } = "🏁\n{0} km\n({1} km)";

            protected override string InitialText => "Nissan\nConnect\nRange";
            protected override string UpdateName => "Range update";

            protected override string FormatStatus(AttributesBatteryStatus status) =>
                string.Format(Format, status.RangeHvacOn, status.RangeHvacOff);
        }

        public class NissanConnectBatteryStatusAge : NissanConnectTile
        {
            [Setting] public string Format { get; set; } = "{0}\n{1}";
            [Setting] public string FormatAge { get; set; } = "hh\\:mm";
            [Setting] public string FormatDateTime { get; set; } = "HH\\:mm";

            protected override string InitialText => "Nissan\nConnect\nBattery\nStatus";
            protected override string UpdateName => "Battery status age update";

            protected override string FormatStatus(AttributesBatteryStatus status) =>
                string.Format(Format, status.BatteryStatusAge?.ToString(FormatAge),
                    status.LastUpdateTime?.ToLocalTime().ToString(FormatDateTime));
        }

        public class NissanConnectChargeStatus : NissanConnectTile
        {
            [Setting] public string FormatNotCharging { get; set; } = "🔋";
            [Setting] public string FormatCharging { get; set; } = "⚡";
            [Setting] public string FormatPluggedIn { get; set; } = "🔌";

            protected override string InitialText => "Nissan\nConnect\nCharge\nStatus";
            protected override string UpdateName => "Charge status update";

            protected override string FormatStatus(AttributesBatteryStatus status) =>
                (status.ChargeStatus, status.PlugStatus) switch
                {
                    (ChargeStatus.NotCharging, PlugStatus.NotPluggedIn) => FormatNotCharging,
                    (ChargeStatus.NotCharging, PlugStatus.PluggedIn) => FormatPluggedIn,
                    (ChargeStatus.Charging, PlugStatus.PluggedIn) => FormatCharging,
                    _ => throw new InvalidOperationException(
                        $"Nissan Connect returned invalid or inconsistent charge/plug states ({status.ChargeStatus}, {status.PlugStatus}).")
                };
        }
    }
}
