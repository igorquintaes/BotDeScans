using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using FluentResults;
using Serilog;
using System.Diagnostics;

namespace BotDeScans.App.Features.Publish.Interaction;

public class SequentialStepRunner(DiscordPublisher discordPublisher)
{
    public async Task<(Result Result, State State, bool ShouldStop)> RunAsync(
        Result aggregate,
        State state,
        IEnumerable<(IStep Step, StepInfo Info)> chain,
        CancellationToken cancellationToken)
        => await RunAsync(aggregate, state, chain, ExecuteStepAsync, cancellationToken);

    public async Task<(Result Result, State State, bool ShouldStop)> RunValidationsAsync(
        Result aggregate,
        State state,
        IEnumerable<(IStep Step, StepInfo Info)> chain,
        CancellationToken cancellationToken)
        => await RunAsync(aggregate, state, chain, ExecuteValidationAsync, cancellationToken);

    private static async Task<(Result Result, State State, bool ShouldStop)> RunAsync(
        Result aggregate,
        State state,
        IEnumerable<(IStep Step, StepInfo Info)> chain,
        Func<(IStep Step, StepInfo Info), State, CancellationToken, Task<Result<State>>> stepAction,
        CancellationToken cancellationToken)
    {
        foreach (var item in chain)
        {
            var stepResult = await stepAction(item, state, cancellationToken);
            if (stepResult.IsSuccess)
                state = stepResult.Value;

            aggregate = Result.Merge(aggregate, stepResult.ToResult());

            if (stepResult.IsFailed && item.Step.ContinueOnError is false)
                return (aggregate, state, true);
        }

        return (aggregate, state, false);
    }

    private async Task<Result<State>> ExecuteStepAsync(
        (IStep Step, StepInfo Info) data,
        State state,
        CancellationToken cancellationToken)
    {
        if (data.Info.Status == StepStatus.Skip)
            return Result.Ok(state);

        var stopwatch = Stopwatch.StartNew();
        var result = await data.Step.SafeCallAsync(x => x.ExecuteAsync(state, cancellationToken));
        stopwatch.Stop();

        Log.Information(
            "Publish step '{StepName}' ExecuteAsync finished in {ElapsedMilliseconds} ms with status {Status}.",
            data.Step.Name,
            stopwatch.ElapsedMilliseconds,
            result.IsSuccess ? "Success" : "Failure");

        var handleResult = await NotifyAsync(result.ToResult(), data.Step, state, cancellationToken);
        return handleResult.IsFailed
            ? handleResult
            : Result.Ok(result.IsSuccess ? result.Value with { Steps = handleResult.Value.Steps } : handleResult.Value);
    }

    private async Task<Result<State>> ExecuteValidationAsync(
        (IStep Step, StepInfo Info) data,
        State state,
        CancellationToken cancellationToken)
    {
        if (data.Info.Status == StepStatus.Skip)
            return Result.Ok(state);

        var result = await ((IPublishStep)data.Step).SafeCallAsync(x => x.ValidateAsync(state, cancellationToken));
        return await NotifyAsync(result, data.Step, state, cancellationToken);
    }

    private async Task<Result<State>> NotifyAsync(
        Result result,
        IStep step,
        State state,
        CancellationToken cancellationToken)
    {
        var updatedInfo = state.Steps[step].UpdateStatus(result);
        var updatedState = state with { Steps = state.Steps.WithUpdatedStepInfo(step, updatedInfo) };

        var feedbackResult = await discordPublisher.SynchronizedUpdateTrackingMessageAsync(updatedState, cancellationToken);
        if (feedbackResult.IsFailed)
            return Result.Merge(result, feedbackResult.ToResult()).ToResult<State>();

        var merged = Result.Merge(result, feedbackResult.ToResult());
        return merged.IsFailed
            ? merged.ToResult<State>()
            : feedbackResult;
    }
}
