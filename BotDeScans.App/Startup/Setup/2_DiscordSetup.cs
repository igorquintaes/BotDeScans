using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Initializations;
using FluentResults;
using FluentValidation;

namespace BotDeScans.App.Startup.Setup;

public class DiscordSetup(SetupDiscordService setupDiscordService, IValidator<SetupDiscordService> validator) : ISetup
{
    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var aggregateResult = Result.Ok();

        aggregateResult.WithReasons(await RunValidationSetupAsync(cancellationToken));
        if (aggregateResult.IsFailed)
            return aggregateResult;

        aggregateResult.WithReasons(await RunCommandsSetupAsync(cancellationToken));
        if (aggregateResult.IsFailed)
            return aggregateResult;

        return aggregateResult.WithSuccess("Discord setup completed successfully.");
    }

    private async Task<List<IReason>> RunValidationSetupAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Discord Validation check...");

        var validationResult = await validator.ValidateAsync(setupDiscordService, cancellationToken);
        return validationResult.ToResult().Reasons;
    }

    private async Task<List<IReason>> RunCommandsSetupAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Discord Commands update...");

        var discordUpdateResult = await setupDiscordService.SetupAsync(cancellationToken);
        return discordUpdateResult.Reasons;
    }
}