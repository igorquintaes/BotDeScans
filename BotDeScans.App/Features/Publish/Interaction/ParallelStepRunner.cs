using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using FluentResults;
using Serilog;
using System.Diagnostics;

namespace BotDeScans.App.Features.Publish.Interaction;

public class ParallelStepRunner(DiscordPublisher discordPublisher)
{
    // Runs conversion steps in parallel using Task.WhenAll.
    // Each step reads from State.OriginContentFolder (read-only) and writes to its own isolated
    // directory, so there are no write conflicts between concurrent tasks.
    public async Task<(Result Result, State State, bool ShouldStop)> RunConversionAsync(
        Result aggregate,
        State state,
        IEnumerable<(IConversionStep Step, StepInfo Info)> conversionSteps,
        CancellationToken cancellationToken)
    {
        var items = conversionSteps.ToList();
        var tracker = new ParallelStepsTracker(state, discordPublisher.SynchronizedUpdateTrackingMessageAsync);

        var results = await Task.WhenAll(
            items.Select(item => ExecuteStepAsync((item.Step, item.Info), state, tracker, cancellationToken)));

        foreach (var stepResult in results)
            aggregate = Result.Merge(aggregate, stepResult);

        aggregate = Result.Merge(aggregate, tracker.AggregateTrackingResult);

        var hasFatalFailure = results
            .Zip(items, (r, item) => (Result: r, Step: (IStep)item.Step))
            .Any(x => x.Result.IsFailed && !x.Step.ContinueOnError)
            || tracker.AggregateTrackingResult.IsFailed;

        return (aggregate, tracker.CurrentState, hasFatalFailure);
    }

    // Runs publish steps grouped by Dependency, each group in parallel, groups sequentially.
    public async Task<(Result Result, State State)> RunDagAsync(
        Result aggregate,
        State state,
        IEnumerable<(IPublishStep Step, StepInfo Info)> publishSteps,
        CancellationToken cancellationToken)
    {
        // Group by dependency: steps with a dependency run before null-dependency steps.
        var groups = publishSteps
            .GroupBy(x => x.Step.Dependency)
            .OrderBy(g => g.Key.HasValue ? 0 : 1)
            .ToList();

        foreach (var group in groups)
        {
            var groupItems = group.ToList();
            var tracker = new ParallelStepsTracker(state, discordPublisher.SynchronizedUpdateTrackingMessageAsync);

            var results = await Task.WhenAll(
                groupItems.Select(item =>
                    ExecuteStepAsync((item.Step, item.Info), state, tracker, cancellationToken)));

            foreach (var stepResult in results)
                aggregate = Result.Merge(aggregate, stepResult);

            aggregate = Result.Merge(aggregate, tracker.AggregateTrackingResult);
            state = tracker.CurrentState;

            var hasFatalFailure = results
                .Zip(groupItems, (r, item) => (Result: r, item.Step))
                .Any(x => x.Result.IsFailed && !x.Step.ContinueOnError)
                || tracker.AggregateTrackingResult.IsFailed;

            if (hasFatalFailure)
                return (aggregate, state);
        }

        return (aggregate, state);
    }

    // Executes the step without a lock (the slow part), then atomically merges its output
    // into the shared tracker state and sends a real-time Discord tracking update.
    private static async Task<Result> ExecuteStepAsync(
        (IStep Step, StepInfo Info) data,
        State initialState,
        ParallelStepsTracker tracker,
        CancellationToken cancellationToken)
    {
        if (data.Info.Status == StepStatus.Skip)
            return Result.Ok();

        var stopwatch = Stopwatch.StartNew();
        var result = await data.Step.SafeCallAsync(x => x.ExecuteAsync(initialState, cancellationToken));
        stopwatch.Stop();

        Log.Information(
            "Publish step '{StepName}' ExecuteAsync finished in {ElapsedMilliseconds} ms with status {Status}.",
            data.Step.Name,
            stopwatch.ElapsedMilliseconds,
            result.IsSuccess ? "Success" : "Failure");

        var stepSnapshot = result.IsSuccess ? result.Value : initialState;
        await tracker.ApplyAndNotifyAsync(result.ToResult(), data.Step, stepSnapshot, cancellationToken);
        return result.ToResult();
    }
}
