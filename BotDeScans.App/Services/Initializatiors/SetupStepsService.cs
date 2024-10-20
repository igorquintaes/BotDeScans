using BotDeScans.App.Services.Logging;
using Microsoft.Extensions.Hosting;
namespace BotDeScans.App.Services.Initializatiors;

public class SetupStepsService(StepsService stepsService, LoggerService loggerService) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Validating steps - Started");
        var loadResult = stepsService.ValidateStepsDependencies();
        if (loadResult.IsFailed)
            loggerService.LogErrors("Failed to validate steps.", loadResult.Errors);

        Console.WriteLine("Validating steps - Done");
        return Task.CompletedTask;
    }
}