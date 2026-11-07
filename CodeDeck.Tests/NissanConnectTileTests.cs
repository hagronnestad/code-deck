using CodeDeck.PluginAbstractions;
using CodeDeck.Plugins.Plugins.Runner;
using Microsoft.Extensions.Logging.Abstractions;
using NissanConnectLib.Enums;
using NissanConnectLib.Models;

namespace CodeDeck.Tests;

public class NissanConnectTileTests
{
    [Theory]
    [InlineData(ChargeStatus.NotCharging, PlugStatus.NotPluggedIn, "idle")]
    [InlineData(ChargeStatus.NotCharging, PlugStatus.PluggedIn, "plugged")]
    [InlineData(ChargeStatus.Charging, PlugStatus.PluggedIn, "charging")]
    public void ChargeStatusDistinguishesValidStates(ChargeStatus charge, PlugStatus plug, string expected)
    {
        var tile = new ChargeTile
        {
            FormatNotCharging = "idle",
            FormatPluggedIn = "plugged",
            FormatCharging = "charging"
        };

        Assert.Equal(expected, tile.RenderStatus(new AttributesBatteryStatus { ChargeStatus = charge, PlugStatus = plug }));
    }

    [Theory]
    [InlineData(ChargeStatus.Charging, PlugStatus.NotPluggedIn)]
    [InlineData(ChargeStatus.Error, PlugStatus.PluggedIn)]
    [InlineData(ChargeStatus.NotCharging, PlugStatus.Error)]
    [InlineData((ChargeStatus)999, PlugStatus.PluggedIn)]
    [InlineData(ChargeStatus.NotCharging, (PlugStatus)999)]
    public void ChargeStatusRejectsInvalidOrContradictoryStates(ChargeStatus charge, PlugStatus plug)
    {
        var tile = new ChargeTile();

        Assert.Throws<InvalidOperationException>(() =>
            tile.RenderStatus(new AttributesBatteryStatus { ChargeStatus = charge, PlugStatus = plug }));
    }

    [Fact]
    public void BatteryStatusAgeUsesTimeSpanFormatting()
    {
        var tile = new AgeTile { Format = "{0}", FormatAge = "hh\\:mm" };
        var status = new AttributesBatteryStatus
        {
            LastUpdateTime = DateTimeOffset.UtcNow.AddHours(-2).AddMinutes(-15)
        };

        Assert.Equal("02:15", tile.RenderStatus(status));
        tile.FormatAge = "HH\\:mm";
        Assert.Throws<FormatException>(() => tile.RenderStatus(status));
    }

    [Fact]
    public async Task FailedAuthenticationCanBeRetriedAndCancellationLeavesIndicatorOff()
    {
        var directory = Directory.CreateTempSubdirectory("CodeDeck-NissanConnectTests-");
        var context = new PluginContext(directory.FullName, null, CancellationToken.None, NullLoggerFactory.Instance);
        var tile = new NissanConnect.NissanConnectBatteryLevelTile { FormatError = "error" };
        try
        {
            await new NissanConnect().InitializePluginAsync(context, CancellationToken.None);
            await tile.InitializeAsync(context, CancellationToken.None);

            for (var attempt = 0; attempt < 2; attempt++)
            {
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    tile.OnTilePressDown(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
                Assert.Contains("requires an email and password", exception.Message);
                Assert.Equal("error", tile.Text);
                Assert.False(tile.ShowIndicator);
            }

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tile.OnTilePressDown(cancellation.Token));
            Assert.Equal("error", tile.Text);
            Assert.False(tile.ShowIndicator);
        }
        finally
        {
            await tile.DeinitializeAsync();
            await context.DisposeSharedStateAsync();
            directory.Delete();
        }
    }

    private sealed class ChargeTile : NissanConnect.NissanConnectChargeStatus
    {
        public string RenderStatus(AttributesBatteryStatus status) => FormatStatus(status);
    }

    private sealed class AgeTile : NissanConnect.NissanConnectBatteryStatusAge
    {
        public string RenderStatus(AttributesBatteryStatus status) => FormatStatus(status);
    }
}
