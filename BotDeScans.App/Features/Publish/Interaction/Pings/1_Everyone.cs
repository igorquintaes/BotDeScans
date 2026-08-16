using FluentResults;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Features.Publish.Interaction.Pings;

public class EveryonePing(IConfiguration configuration) : Ping(configuration)
{
    protected override PingType Type => PingType.Everyone;

    public override Task<Result<string>> GetPingAsTextAsync(State state, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Ok("@everyone"));
}
