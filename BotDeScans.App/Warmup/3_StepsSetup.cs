using BotDeScans.App.Services.Initializations;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace BotDeScans.App.Warmup;

public static class StepsSetup
{
    public static Result Run(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine("Steps Warming Up...");

            var setupStepsService = scope.ServiceProvider.GetRequiredService<SetupStepsService>();
            var setupStepsResult = setupStepsService.Setup();
            return setupStepsResult;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Steps setup warmup EXCEPTIONAL ERROR!");

            var error = new ExceptionalError("An exceptional error occurred during Steps setup.", ex);
            return Result.Fail(error);
        }
    }
}
