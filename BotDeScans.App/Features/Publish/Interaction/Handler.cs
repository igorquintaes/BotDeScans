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

        var result = await discordPublisher.UpdateTrackingMessageAsync(state.Steps, cancellationToken);
        if (result.IsFailed)
            return result.ToResult<State>();

        var currentState = state;

        var managementExecution = await ExecuteChainAsync(
            result,
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
                : Result.Ok(currentState);

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

    private async Task<(Result Result, bool ShouldStop)> ExecuteValidationChainAsync(
        Result aggregate,
        State state,
        IEnumerable<(IPublishStep Step, StepInfo Info)> chain,
        CancellationToken cancellationToken)
    {
        foreach (var (Step, Info) in chain)
        {
            var stepResult = await ValidateAsync((Step, Info), state, cancellationToken);
            aggregate = Result.Merge(aggregate, stepResult);

            if (stepResult.IsFailed && Step.ContinueOnError is false)
                return (aggregate, true);
        }

        return (aggregate, false);
    }

    private async Task<Result> ValidateAsync(
        (IPublishStep Step, StepInfo Info) data,
        State state,
        CancellationToken cancellationToken)
    {
        if (data.Info.Status == StepStatus.Skip)
            return Result.Ok();

        var result = await data.Step.SafeCallAsync(x => x.ValidateAsync(state, cancellationToken));
        return await HandleResult(result, data.Info, state, cancellationToken);
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

        var handleResult = await HandleResult(result.ToResult(), data.Info, state, cancellationToken);
        return handleResult.IsFailed
            ? handleResult.ToResult<State>()
            : Result.Ok(result.Value);
    }

    private async Task<Result> HandleResult(
        Result result,
        StepInfo info,
        State state,
        CancellationToken cancellationToken)
    {
        info.UpdateStatus(result);

        var feedbackResult = await discordPublisher.UpdateTrackingMessageAsync(state.Steps, cancellationToken);
        return Result.Merge(result, feedbackResult);
    }
}
