using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Features.Publish.Interaction.Pings;

public class EveryonePing(IConfiguration configuration) : Ping(configuration)
{
    protected override PingType Type => PingType.Everyone;

    public override Task<string> GetPingAsTextAsync(CancellationToken cancellationToken) =>
        Task.FromResult("@everyone");
}
