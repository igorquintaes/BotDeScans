using FluentResults;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Features.Publish.Pings;

public class NonePing(IConfiguration configuration) : Ping(configuration)
{
    protected override PingType Type => PingType.None;

    public override Task<Result<string>> GetPingAsTextAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Result.Ok(string.Empty));
}
