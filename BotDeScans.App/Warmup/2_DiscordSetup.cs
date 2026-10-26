using BotDeScans.App.Services.Initializations;
using FluentResults;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BotDeScans.App.Warmup;

public static class DiscordSetup
{
    public static async Task<Result> RunAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var aggregateResult = Result.Ok();
        try
        {
            aggregateResult.WithReasons(await RunValidationSetupAsync(scope, cancellationToken));
            if (aggregateResult.IsFailed)
                return aggregateResult;

            return aggregateResult.WithReasons(await RunCommandsSetupAsync(scope, cancellationToken));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Discord setup warmup EXCEPTIONAL ERROR!");

            var error = new ExceptionalError("An exceptional error occurred during Discord setup.", ex);
            aggregateResult.WithError(error);
        }

        return aggregateResult;
    }

    private static async Task<List<IReason>> RunValidationSetupAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        Console.WriteLine("Discord Validation Warming Up...");

        var setupDiscordService = scope.ServiceProvider.GetRequiredService<SetupDiscordService>();
        var setupDiscordValidator = scope.ServiceProvider.GetRequiredService<IValidator<SetupDiscordService>>();
        var validationResult = await setupDiscordValidator.ValidateAsync(setupDiscordService, cancellationToken);
        return validationResult.ToResult().Reasons;
    }

    private static async Task<List<IReason>> RunCommandsSetupAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        Console.WriteLine("Discord Commands Warming Up...");

        var setupDiscordService = scope.ServiceProvider.GetRequiredService<SetupDiscordService>();
        var discordUpdateResult = await setupDiscordService.SetupAsync(cancellationToken);
        return discordUpdateResult.Reasons;
    }
}
