using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace BotDeScans.App.Warmup;

public interface IDiscordSetup
{
    Task<Result> RunAsync(IServiceScope scope, CancellationToken cancellationToken);
}
