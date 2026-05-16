using BotDeScans.App.Features.Publish.Interaction.Steps;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction;

public class Handler(
    DiscordPublisher discordPublisher,
    SequentialStepRunner sequentialRunner,
    ParallelStepRunner parallelRunner)
{
    public virtual async Task<Result<State>> ExecuteAsync(State state, CancellationToken cancellationToken)
    {
        var managementSteps = state.Steps.ManagementSteps.ToList();
        var conversionSteps = state.Steps.ConversionSteps.ToList();
        var publishSteps = state.Steps.PublishSteps.ToList();

        var trackingResult = await discordPublisher.UpdateTrackingMessageAsync(state, cancellationToken);
        if (trackingResult.IsFailed)
            return trackingResult;

        var currentState = trackingResult.Value;
        var currentResult = trackingResult.ToResult();

        // Phase 1: Setup, Download, Compress — strictly sequential.
        var managementExecution = await sequentialRunner.RunAsync(
            currentResult,
            currentState,
            managementSteps.Select(data => ((IStep)data.Step, data.Info)),
            cancellationToken);
        if (managementExecution.ShouldStop)
            return ToFinalResult(managementExecution.Result, managementExecution.State);

        currentState = managementExecution.State;
        currentResult = managementExecution.Result;

        // Phase 2: Conversion steps (ZipFiles, PdfFiles, …) — run in parallel.
        var conversionExecution = await parallelRunner.RunConversionAsync(
            currentResult,
            currentState,
            conversionSteps,
            cancellationToken);
        if (conversionExecution.ShouldStop)
            return ToFinalResult(conversionExecution.Result, conversionExecution.State);

        currentState = conversionExecution.State;
        currentResult = conversionExecution.Result;

        // Phase 3: Validate all publish steps sequentially.
        var validationExecution = await sequentialRunner.RunValidationsAsync(
            currentResult,
            currentState,
            publishSteps.Select(data => ((IStep)data.Step, data.Info)),
            cancellationToken);
        if (validationExecution.ShouldStop)
            return ToFinalResult(validationExecution.Result, validationExecution.State);

        // Phase 4: Publish steps — grouped by Dependency, each group in parallel.
        var publishExecution = await parallelRunner.RunDagAsync(
            validationExecution.Result,
            validationExecution.State,
            publishSteps,
            cancellationToken);

        return ToFinalResult(publishExecution.Result, publishExecution.State);
    }

    private static Result<State> ToFinalResult(Result result, State state) =>
        result.IsFailed ? result.ToResult<State>() : Result.Ok(state);
}
