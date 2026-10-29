using BotDeScans.App.Services.Initializations;
using FluentResults;

namespace BotDeScans.App.Startup.Setup;

public class StepsSetup(SetupStepsService setupStepsService) : ISetup
{
    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken) => 
        await Task.FromResult(setupStepsService.Setup());
}