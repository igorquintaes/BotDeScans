using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using FluentResults;
using Serilog;
using System.Diagnostics;

namespace BotDeScans.App.Features.Publish.Interaction;

public class Handler(
    DiscordPublisher discordPublisher)
{
    public virtual async Task<Result<State>> ExecuteAsync(State state, CancellationToken cancellationToken)
    {
        var managementSteps = state.Steps.ManagementSteps;
        var publishSteps = state.Steps.PublishSteps;

        var trackingResult = await discordPublisher.UpdateTrackingMessageAsync(state, cancellationToken);
        if (trackingResult.IsFailed)
            return trackingResult;

        var currentState = trackingResult.Value;

        var managementExecution = await ExecuteChainAsync(
            trackingResult.ToResult(),
            currentState,
            managementSteps.Select(data => (
                Step: (IStep)data.Step, data.Info)),
            cancellationToken);
        if (managementExecution.ShouldStop)
            return managementExecution.Result.IsFailed
                ? managementExecution.Result.ToResult<State>()
                : Result.Ok(managementExecution.State);

        currentState = managementExecution.State;

        var validationExecution = await ExecuteValidationChainAsync(
            managementExecution.Result,
            currentState,
            publishSteps.Select(data => (data.Step, data.Info)),
            cancellationToken);
        if (validationExecution.ShouldStop)
            return validationExecution.Result.IsFailed
                ? validationExecution.Result.ToResult<State>()
                : Result.Ok(validationExecution.State);

        currentState = validationExecution.State;

        var publishExecution = await ExecuteChainAsync(
            validationExecution.Result,
            currentState,
            publishSteps.Select(data => (
                Step: (IStep)data.Step, data.Info)),
            cancellationToken);

        return publishExecution.Result.IsFailed
            ? publishExecution.Result.ToResult<State>()
            : Result.Ok(publishExecution.State);
    }

    private async Task<(Result Result, State State, bool ShouldStop)> ExecuteChainAsync(
        Result aggregate,
        State state,
        IEnumerable<(IStep Step, StepInfo Info)> chain,
        CancellationToken cancellationToken)
    {
        foreach (var (Step, Info) in chain)
        {
            var stepResult = await ExecuteAsync((Step, Info), state, cancellationToken);
            if (stepResult.IsSuccess)
                state = stepResult.Value;

            aggregate = Result.Merge(aggregate, stepResult.ToResult());

            if (stepResult.IsFailed && Step.ContinueOnError is false)
                return (aggregate, state, true);
        }

        return (aggregate, state, false);
    }

    private async Task<(Result Result, State State, bool ShouldStop)> ExecuteValidationChainAsync(
        Result aggregate,
        State state,
        IEnumerable<(IPublishStep Step, StepInfo Info)> chain,
        CancellationToken cancellationToken)
    {
        foreach (var (Step, Info) in chain)
        {
            var stepResult = await ValidateAsync((Step, Info), state, cancellationToken);
            if (stepResult.IsSuccess)
                state = stepResult.Value;

            aggregate = Result.Merge(aggregate, stepResult.ToResult());

            if (stepResult.IsFailed && Step.ContinueOnError is false)
                return (aggregate, state, true);
        }

        return (aggregate, state, false);
    }

    private async Task<Result<State>> ValidateAsync(
        (IPublishStep Step, StepInfo Info) data,
        State state,
        CancellationToken cancellationToken)
    {
        if (data.Info.Status == StepStatus.Skip)
            return Result.Ok(state);

        var result = await data.Step.SafeCallAsync(x => x.ValidateAsync(state, cancellationToken));
        return await HandleResult(result, data.Step, state, cancellationToken);
    }

    private async Task<Result<State>> ExecuteAsync(
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

        var handleResult = await HandleResult(result.ToResult(), data.Step, state, cancellationToken);
        return handleResult.IsFailed
            ? handleResult
            : Result.Ok(result.IsSuccess ? result.Value with { Steps = handleResult.Value.Steps } : handleResult.Value);
    }

    private async Task<Result<State>> HandleResult(
        Result result,
        IStep step,
        State state,
        CancellationToken cancellationToken)
    {
        var updatedInfo = state.Steps[step].UpdateStatus(result);
        var updatedSteps = state.Steps.WithUpdatedStepInfo(step, updatedInfo);
        var updatedState = state with { Steps = updatedSteps };

        var feedbackResult = await discordPublisher.UpdateTrackingMessageAsync(updatedState, cancellationToken);
        if (feedbackResult.IsFailed)
            return Result.Merge(result, feedbackResult.ToResult()).ToResult<State>();

        var merged = Result.Merge(result, feedbackResult.ToResult());
        return merged.IsFailed
            ? merged.ToResult<State>()
            : feedbackResult;
    }
}
