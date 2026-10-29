using BotDeScans.App.Services.Initializations;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace BotDeScans.App.Warmup;

public static class ExternalServicesSetup
{
    public static async Task<Result> RunAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            var setupPublishStepsService = scope.ServiceProvider.GetRequiredService<SetupClientsService>();
            var setupPublishStepsResult = await setupPublishStepsService.SetupAsync(cancellationToken);
            return setupPublishStepsResult;
        }
        catch (Exception ex)
        {
            Console.WriteLine("External Services setup warmup EXCEPTIONAL ERROR!");

            var error = new ExceptionalError("An exceptional error occurred during External Services setup.", ex);
            return Result.Fail(error);
        }
    }
}

public class ExternalServicesSetupWrapper : IExternalServicesSetup
{
    public virtual async Task<Result> RunAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        return await ExternalServicesSetup.RunAsync(scope, cancellationToken);
    }
}
