using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.Steps.Models;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public class StepsService(
    PublishMessageService publishMessageService,
    PublishState publishState)
{
    public virtual async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var chain = publishState.Steps.ManagementSteps.Select(step => (Func<Task<Result>>)(() => ExecuteAsync(step, cancellationToken)))
             .Union(publishState.Steps.PublishSteps.Select(step => (Func<Task<Result>>)(() => ValidateAsync(step, cancellationToken))))
             .Union(publishState.Steps.PublishSteps.Select(step => (Func<Task<Result>>)(() => ExecuteAsync(step, cancellationToken))));

        var result = await publishMessageService.UpdateTrackingMessageAsync(cancellationToken);

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

        var feedbackResult = await publishMessageService.UpdateTrackingMessageAsync(cancellationToken);
        return Result.Merge(result, feedbackResult);
    }
}
