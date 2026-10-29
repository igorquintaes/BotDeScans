using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace BotDeScans.App.Warmup;

public interface IExternalServicesSetup
{
    Task<Result> RunAsync(IServiceScope scope, CancellationToken cancellationToken);
}
