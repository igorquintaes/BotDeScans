using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using FluentResults;
using Serilog;
using System.Diagnostics;

namespace BotDeScans.App.Features.Publish.Interaction;

public class Handler(
    DiscordPublisher discordPublisher,
    State state)
{
    public virtual async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var managementSteps = state.Steps.ManagementSteps;
        var publishSteps = state.Steps.PublishSteps;

        var result = await discordPublisher.UpdateTrackingMessageAsync(cancellationToken);
        if (result.IsFailed)
            return result;

        var managementExecution = await ExecuteChainAsync(
            result,
            managementSteps.Select(data => (
                Step: (IStep)data.Step,
                Execute: (Func<Task<Result>>)(() => ExecuteAsync((data.Step, data.Info), cancellationToken)))));
        if (managementExecution.ShouldStop)
            return managementExecution.Result;

        var validationExecution = await ExecuteChainAsync(
            managementExecution.Result,
            publishSteps.Select(data => (
                Step: (IStep)data.Step,
                Execute: (Func<Task<Result>>)(() => ValidateAsync((data.Step, data.Info), cancellationToken)))));
        if (validationExecution.ShouldStop)
            return validationExecution.Result;

        var publishExecution = await ExecuteChainAsync(
            validationExecution.Result,
            publishSteps.Select(data => (
                Step: (IStep)data.Step,
                Execute: (Func<Task<Result>>)(() => ExecuteAsync((data.Step, data.Info), cancellationToken)))));
        return publishExecution.Result;
    }

    private static async Task<(Result Result, bool ShouldStop)> ExecuteChainAsync(
        Result aggregate,
        IEnumerable<(IStep Step, Func<Task<Result>> Execute)> chain)
    {
        foreach (var (Step, Execute) in chain)
        {
            var stepResult = await Execute();
            aggregate = Result.Merge(aggregate, stepResult);

            if (stepResult.IsFailed && Step.ContinueOnError is false)
                return (aggregate, true);
        }

        return (aggregate, false);
    }

    private async Task<Result> ValidateAsync(
        (IPublishStep Step, StepInfo Info) data,
        CancellationToken cancellationToken)
    {
        if (data.Info.Status == StepStatus.Skip)
            return Result.Ok();

        var result = await data.Step.SafeCallAsync(x => x.ValidateAsync(cancellationToken));
        return await HandleResult(result, data.Info, cancellationToken);
    }

    private async Task<Result> ExecuteAsync(
        (IStep Step, StepInfo Info) data,
        CancellationToken cancellationToken)
    {
        if (data.Info.Status == StepStatus.Skip)
            return Result.Ok();

        var stopwatch = Stopwatch.StartNew();
        var result = await data.Step.SafeCallAsync(x => x.ExecuteAsync(cancellationToken));
        stopwatch.Stop();

        Log.Information(
            "Publish step '{StepName}' ExecuteAsync finished in {ElapsedMilliseconds} ms with status {Status}.",
            data.Step.Name,
            stopwatch.ElapsedMilliseconds,
            result.IsSuccess ? "Success" : "Failure");

        return await HandleResult(result, data.Info, cancellationToken);
    }

    private async Task<Result> HandleResult(
        Result result,
        StepInfo info,
        CancellationToken cancellationToken)
    {
        info.UpdateStatus(result);

        var feedbackResult = await discordPublisher.UpdateTrackingMessageAsync(cancellationToken);
        return Result.Merge(result, feedbackResult);
    }
}
