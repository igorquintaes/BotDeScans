using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Features.Publish.Interaction.Pings;

public class NonePing(IConfiguration configuration) : Ping(configuration)
{
    protected override PingType Type => PingType.None;

    public override Task<string> GetPingAsTextAsync(CancellationToken cancellationToken) =>
        Task.FromResult(string.Empty);
}
