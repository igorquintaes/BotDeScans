using BotDeScans.App.Features.GoogleDrive.InternalServices;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
namespace BotDeScans.App.Services.Initializations;

public class SetupClientsService(IServiceProvider serviceProvider)
{
    public async Task<Result> SetupAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Initalizing External Clients...");

        var aggregatedResult = Result.Ok();
        foreach (var factoryType in Assembly
            .GetEntryAssembly()!
            .GetTypes()
            .Where(type => type.BaseType is not null
                        && type.BaseType.IsGenericType
                        && type.BaseType.GetGenericTypeDefinition() == typeof(ClientFactory<>)))
        {
            dynamic factory = serviceProvider.GetRequiredService(factoryType);
            if (factory.ExpectedInPublishFeature is false)
                continue;

            var validationResult = factory.ValidateConfiguration();
            aggregatedResult = aggregatedResult.WithReasons(validationResult.Reasons);
            if (aggregatedResult.IsFailed)
                continue;

            var clientResult = await factory.SafeCreateAsync(cancellationToken);
            aggregatedResult = aggregatedResult.WithReasons(clientResult.Reasons);
            if (aggregatedResult.IsFailed)
                continue;

            var healthCheckResult = await factory.HealthCheckAsync(clientResult.Value, cancellationToken);
            aggregatedResult = aggregatedResult.WithReasons(healthCheckResult.Reasons);
            if (aggregatedResult.IsFailed)
                continue;
        }

        if (aggregatedResult.IsFailed)
            return aggregatedResult;

        Console.WriteLine("Setting up Google Drive Base Folder...");

        return await serviceProvider
            .GetRequiredService<GoogleDriveSettingsService>()
            .SetUpBaseFolderAsync(cancellationToken);
    }
}
