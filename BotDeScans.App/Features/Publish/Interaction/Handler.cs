using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction;

public class Handler(
    DiscordPublisher discordPublisher,
    State state)
{
    public virtual async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var managementSteps = state.Steps.ManagementSteps;
        var publishSteps = state.Steps.PublishSteps;

        var chain = managementSteps.Select(x => (Func<Task<Result>>)(() => ExecuteAsync(x, cancellationToken)))
             .Union(publishSteps.Select(x => (Func<Task<Result>>)(() => ValidateAsync(x, cancellationToken))))
             .Union(publishSteps.Select(x => (Func<Task<Result>>)(() => ExecuteAsync(x, cancellationToken))));

        var result = await discordPublisher.UpdateTrackingMessageAsync(cancellationToken);

        foreach (var execStep in chain)
        {
            if (result.IsFailed) break;
            result = Result.Merge(result, await execStep());
        }

        return result;

    }

    private async Task<Result> ValidateAsync(
        (IPublishStep Step, StepInfo Info) data,
        CancellationToken cancellationToken)
    {
        var result = await data.Step.SafeCallAsync(x => x.ValidateAsync(cancellationToken));
        return await HandleResult(result, data.Info, cancellationToken);
    }

    private async Task<Result> ExecuteAsync(
        (IStep Step, StepInfo Info) data,
        CancellationToken cancellationToken)
    {
        var result = await data.Step.SafeCallAsync(x => x.ExecuteAsync(cancellationToken));
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
