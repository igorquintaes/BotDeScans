using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace BotDeScans.App.Warmup;

public interface ISetup
{
    Task<Result> Run(IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
