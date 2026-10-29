using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace BotDeScans.App.Warmup;

public interface IDatabaseSetup
{
    Task<Result> RunAsync(AsyncServiceScope scope, CancellationToken cancellationToken);
}
