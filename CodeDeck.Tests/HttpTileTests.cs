using System.Net;
using CodeDeck.PluginAbstractions;
using CodeDeck.Plugins.Plugins.Runner;
using WebRequestPlugin = CodeDeck.Plugins.Plugins.WebRequest.WebRequest;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CodeDeck.Tests;

public class HttpTileTests
{
    [Fact]
    public async Task RunnerInitializationCancelsItsFaviconRequest()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handler = new StubHandler(async token =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler);
        var context = CreateContext("Runner.HttpClient", client);
        var tile = new Runner.OpenWebsiteTile { Url = "https://example.com" };
        using var cancellation = new CancellationTokenSource();
        var initialization = tile.InitializeAsync(context, cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => initialization.WaitAsync(TimeSpan.FromSeconds(5)));
        await tile.DeinitializeAsync();
        await context.DisposeSharedStateAsync();
    }

    [Fact]
    public async Task RunnerReleasesDownloadedFaviconOnTeardown()
    {
        using var image = new Image<Rgba32>(2, 2);
        using var buffer = new MemoryStream();
        image.SaveAsPng(buffer);
        using var handler = new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(buffer.ToArray())
        }));
        using var client = new HttpClient(handler);
        var context = CreateContext("Runner.HttpClient", client);
        var tile = new Runner.OpenWebsiteTile { Url = "https://example.com" };
        await tile.InitializeAsync(context, CancellationToken.None);
        var favicon = Assert.IsAssignableFrom<Image>(tile.Image);

        await tile.DeinitializeAsync();
        Assert.Throws<ObjectDisposedException>(() => favicon.CloneAs<Rgba32>());
        await context.DisposeSharedStateAsync();
    }

    [Fact]
    public async Task WebRequestCanRetryAfterHttpFailure()
    {
        var attempts = 0;
        using var handler = new StubHandler(_ => Task.FromResult(++attempts == 1
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("Recovered") }));
        using var client = new HttpClient(handler);
        var context = CreateContext("WebRequest.HttpClient", client);
        var tile = new WebRequestPlugin.PlainTextTile { Url = "https://example.com" };
        await tile.InitializeAsync(context, CancellationToken.None);

        await Assert.ThrowsAsync<HttpRequestException>(() => tile.OnTilePressUp(CancellationToken.None));
        Assert.False(tile.ShowIndicator);
        await tile.OnTilePressUp(CancellationToken.None);
        Assert.Equal("Recovered", tile.Text);
        await tile.DeinitializeAsync();
        await context.DisposeSharedStateAsync();
    }

    private static PluginContext CreateContext(string key, HttpClient client)
    {
        var context = new PluginContext(".", null, CancellationToken.None, NullLoggerFactory.Instance);
        context.GetOrCreateSharedState(key, () => client);
        return context;
    }

    private sealed class StubHandler(Func<CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(cancellationToken);
    }
}
