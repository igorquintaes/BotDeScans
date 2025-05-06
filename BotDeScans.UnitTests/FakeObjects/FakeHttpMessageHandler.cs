namespace BotDeScans.UnitTests.FakeObjects;

public abstract class FakeHttpMessageHandler : HttpMessageHandler
{
    public abstract Task<HttpResponseMessage> FakeSendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken);

    protected sealed override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        => this.FakeSendAsync(request, cancellationToken);
}
