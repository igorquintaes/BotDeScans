using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Initializations.Factories.Base;
using FluentResults;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BotDeScans.App.Services.Initializations.Setups;

public class ClientsSetup(IServiceProvider serviceProvider) : ISetup
{
    public virtual async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Initalizing External Clients...");

        var aggregatedResult = Result.Ok();
        var factoryDataList = GetEnabledFactoriesData();

        await Parallel.ForEachAsync(factoryDataList, cancellationToken, async (factoryData, ct) =>
        {
            var factoryName = factoryData.factory.Name;

            try
            {
                Console.WriteLine("Validating " + factoryName);
                var validationResult = await factoryData.validator.ValidateAsync(new ValidationContext<IClientFactory>(factoryData.factory), cancellationToken);
                var validationFluentResult = FluentValidationExtensions.ToResult(validationResult);
                aggregatedResult.WithReasons(validationFluentResult.Reasons);
                if (validationFluentResult.IsFailed)
                {
                    aggregatedResult.WithError($"Validation failed for {factoryName}");
                    return;
                }

                Console.WriteLine("Creating " + factoryName);
                var clientResult = await factoryData.factory.SafeCreateObjectAsync(cancellationToken);
                aggregatedResult.WithReasons(clientResult.Reasons);
                if (clientResult.IsFailed)
                {
                    aggregatedResult.WithError($"Object creation failed for {factoryName}");
                    return;
                }

                Console.WriteLine("Testing " + factoryName);
                var healthCheckResult = await factoryData.factory.HealthCheckAsync(clientResult.Value, cancellationToken);
                aggregatedResult.WithReasons(healthCheckResult.Reasons);
                if (healthCheckResult.IsFailed)
                    aggregatedResult.WithError($"Health check failed for {factoryName}");

                aggregatedResult.WithSuccess($"{factoryName} executed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{factoryName} EXCEPTIONAL ERROR!");

                var error = new ExceptionalError($"An exceptional error occurred during {factoryName} execution.", ex);
                aggregatedResult.WithError(error);
            }
        });

        return aggregatedResult;
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
