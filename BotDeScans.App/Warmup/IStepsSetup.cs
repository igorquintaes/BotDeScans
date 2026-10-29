using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace BotDeScans.App.Warmup;

public interface IStepsSetup
{
    Result Run(IServiceScope scope, CancellationToken cancellationToken);
}
