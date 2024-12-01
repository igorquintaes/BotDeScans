using BotDeScans.App.Features.Publish.Steps;
using FluentResults;
namespace BotDeScans.App.Services.Initializatiors;

public class SetupStepsService(StepsService stepsService)
{
    public Result Setup()
    {
        Console.WriteLine("Validating Publish Steps...");
        return stepsService.ValidateStepsDependencies();
    }
}