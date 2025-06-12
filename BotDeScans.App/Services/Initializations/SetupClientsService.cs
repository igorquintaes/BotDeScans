using BotDeScans.App.Extensions;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services.Initializations.Factories.Base;
using FluentResults;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
namespace BotDeScans.App.Services.Initializations;

public class SetupClientsService(IServiceProvider serviceProvider)
{
    public async Task<Result> SetupAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Initalizing External Clients...");

        var aggregatedResult = Result.Ok();
        foreach (var (factory, factoryValidator) in GetEnabledFactoriesData())
        {
            var validationResult = await factoryValidator.ValidateAsync(factory, cancellationToken); 
            aggregatedResult = aggregatedResult.WithReasons(FluentResultExtensions.ToResult(validationResult).Reasons);
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

    private IEnumerable<(dynamic factory, dynamic validator)> GetEnabledFactoriesData()
    {
        var factoryTypes = Assembly
            .GetEntryAssembly()!
            .GetTypes()
            .Where(type => type.BaseType is not null 
                        && type.BaseType.IsGenericType is true
                        && type.BaseType.GetGenericTypeDefinition() == typeof(ClientFactory<>));

        foreach (var factoryType in factoryTypes)
        {
            dynamic factory = serviceProvider.GetRequiredService(factoryType);
            if ((bool)factory.Enabled is false)
                continue;

            var factoryValidatorType = typeof(IValidator<>).MakeGenericType(factoryType);
            dynamic factoryValidator = serviceProvider.GetRequiredService(factoryValidatorType);

            yield return (factory, factoryValidator);
        }
    }
}
