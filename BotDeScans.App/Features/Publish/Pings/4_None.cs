using BotDeScans.App.Features.Publish.Discord;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Features.Publish.Pings;

public class NonePing(IConfiguration configuration) : Ping(configuration)
{
    protected override PingType Type => PingType.None;

    public override Task<string> GetPingAsTextAsync(CancellationToken cancellationToken) =>
        Task.FromResult(string.Empty);
}
