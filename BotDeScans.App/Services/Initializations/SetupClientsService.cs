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
            var validationResult = await factoryValidator.ValidateAsync(new ValidationContext<IClientFactory>(factory), cancellationToken); 
            aggregatedResult = aggregatedResult.WithReasons(FluentValidationExtensions.ToResult(validationResult).Reasons);
            if (aggregatedResult.IsFailed)
                continue;

            var clientResult = await factory.SafeCreateObjectAsync(cancellationToken);
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

    private IEnumerable<(IClientFactory factory, IValidator validator)> GetEnabledFactoriesData()
    {
        var factoryTypes = Assembly
            .GetEntryAssembly()!
            .GetTypes()
            .Where(type => type.BaseType is not null 
                        && type.BaseType.IsGenericType is true
                        && type.BaseType.GetGenericTypeDefinition() == typeof(ClientFactory<>));

        foreach (var factoryType in factoryTypes)
        {
            var factory = (IClientFactory)serviceProvider.GetRequiredService(factoryType);
            if (!factory.Enabled)
                continue;

            var factoryValidatorType = typeof(IValidator<>).MakeGenericType(factoryType);
            var factoryValidator = (IValidator)serviceProvider.GetRequiredService(factoryValidatorType);

            yield return (factory, factoryValidator);
        }
    }
}
