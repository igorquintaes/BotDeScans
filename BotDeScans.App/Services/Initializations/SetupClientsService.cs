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

        var factoryTypes = Assembly
            .GetEntryAssembly()!
            .GetTypes()
            .Where(type => type.BaseType is not null
                        && type.BaseType.IsGenericType
                        && type.BaseType.GetGenericTypeDefinition() == typeof(ClientFactory<>));

        var aggregatedResult = Result.Ok();
        foreach (var factoryType in factoryTypes)
        {
            dynamic factory = serviceProvider.GetRequiredService(factoryType);
            if (factory.ExpectedInPublishFeature is false)
                continue;

            var validationResult = factory.ValidateConfiguration();
            if (validationResult.IsFailed)
            {
                aggregatedResult = aggregatedResult.WithErrors(validationResult.Errors);
                continue;
            }

            var clientResult = await factory.SafeCreateAsync(cancellationToken);
            if (clientResult.IsFailed)
            {
                aggregatedResult = aggregatedResult.WithErrors(clientResult.Errors);
                continue;
            }

            var healthCheckResult = await factory.HealthCheckAsync(clientResult.Value, cancellationToken);
            if (healthCheckResult.IsFailed)
            {
                aggregatedResult = aggregatedResult.WithErrors(healthCheckResult.Errors);
                continue;
            }
        }

        if (aggregatedResult.IsFailed)
            return aggregatedResult;

        Console.WriteLine("Setting up Google Drive Base Folder...");

        return await serviceProvider
            .GetRequiredService<GoogleDriveSettingsService>()
            .SetUpBaseFolderAsync(cancellationToken);
    }
}
