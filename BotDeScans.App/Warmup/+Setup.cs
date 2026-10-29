using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace BotDeScans.App.Warmup;

public static class Setup
{
    public static async Task<Result> Run(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var aggregateResult = Result.Ok();

        // Database setup
        var databaseResult = await DatabaseSetup.RunAsync(scope, cancellationToken);
        aggregateResult.WithReasons(databaseResult.Reasons);
        if (aggregateResult.IsFailed)
            return aggregateResult;

        aggregateResult.WithSuccess("Database setup completed successfully.");

        // Discord setup
        var discordResult = await DiscordSetup.RunAsync(scope, cancellationToken);
        aggregateResult.WithReasons(discordResult.Reasons);
        if (aggregateResult.IsFailed)
            return aggregateResult;

        aggregateResult.WithSuccess("Discord setup completed successfully.");

        // Steps setup
        var stepsResult = StepsSetup.Run(scope, cancellationToken);
        aggregateResult.WithReasons(stepsResult.Reasons);
        if (aggregateResult.IsFailed)
            return aggregateResult;

        aggregateResult.WithSuccess("Steps setup completed successfully.");

        // External Services setup
        var externalServicesResult = await ExternalServicesSetup.RunAsync(scope, cancellationToken);
        aggregateResult.WithReasons(externalServicesResult.Reasons);
        if (aggregateResult.IsFailed)
            return aggregateResult;

        aggregateResult.WithSuccess("External Services setup completed successfully.");

        return aggregateResult;
    }
}

public class SetupService : ISetup
{
    private readonly IDatabaseSetup databaseSetup;
    private readonly IDiscordSetup discordSetup;
    private readonly IStepsSetup stepsSetup;
    private readonly IExternalServicesSetup externalServicesSetup;

    public SetupService(
        IDatabaseSetup databaseSetup,
        IDiscordSetup discordSetup,
        IStepsSetup stepsSetup,
        IExternalServicesSetup externalServicesSetup)
    {
        this.databaseSetup = databaseSetup;
        this.discordSetup = discordSetup;
        this.stepsSetup = stepsSetup;
        this.externalServicesSetup = externalServicesSetup;
    }

    public virtual async Task<Result> Run(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var aggregateResult = Result.Ok();

        // Database setup
        var databaseResult = await databaseSetup.RunAsync(scope, cancellationToken);
        aggregateResult.WithReasons(databaseResult.Reasons);
        if (aggregateResult.IsFailed)
            return aggregateResult;

        aggregateResult.WithSuccess("Database setup completed successfully.");

        // Discord setup
        var discordResult = await discordSetup.RunAsync(scope, cancellationToken);
        aggregateResult.WithReasons(discordResult.Reasons);
        if (aggregateResult.IsFailed)
            return aggregateResult;

        aggregateResult.WithSuccess("Discord setup completed successfully.");

        // Steps setup
        var stepsResult = stepsSetup.Run(scope, cancellationToken);
        aggregateResult.WithReasons(stepsResult.Reasons);
        if (aggregateResult.IsFailed)
            return aggregateResult;

        aggregateResult.WithSuccess("Steps setup completed successfully.");

        // External Services setup
        var externalServicesResult = await externalServicesSetup.RunAsync(scope, cancellationToken);
        aggregateResult.WithReasons(externalServicesResult.Reasons);
        if (aggregateResult.IsFailed)
            return aggregateResult;

        aggregateResult.WithSuccess("External Services setup completed successfully.");

        return aggregateResult;
    }
}
