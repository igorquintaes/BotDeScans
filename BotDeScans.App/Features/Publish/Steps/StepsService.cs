using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.Steps.Models;
using FluentResults;
using Remora.Discord.Commands.Contexts;
using Serilog;
namespace BotDeScans.App.Features.Publish.Steps;

public class StepsService(
    PublishMessageService publishMessageService,
    PublishState publishState)
{
    public virtual async Task<Result> ExecuteAsync(IOperationContext context, CancellationToken cancellationToken)
    {
        var chain = publishState.Steps.ManagementSteps.Select(step => (Func<Task<Result>>)(() => ExecuteAsync(step, context, cancellationToken)))
             .Union(publishState.Steps.PublishSteps.Select(step => (Func<Task<Result>>)(() => ValidateAsync(step, context, cancellationToken))))
             .Union(publishState.Steps.PublishSteps.Select(step => (Func<Task<Result>>)(() => ExecuteAsync(step, context, cancellationToken))));

        var result = await publishMessageService.UpdateTrackingMessageAsync(context, cancellationToken);

        foreach (var execStep in chain)
        {
            if (result.IsFailed) break;
            result = Result.Merge(result, await execStep());
        }

        return result;

    }
    private async Task<Result> ValidateAsync(
        (IPublishStep Step, StepInfo Info) data, 
        IOperationContext context, 
        CancellationToken cancellationToken)
    {
        var result = await SafeCall(() => data.Step.ValidateAsync(cancellationToken));
        data.Info.UpdateStatus(result);

        if (result.IsSuccess)
            return result;

        var feedbackResult = await publishMessageService.UpdateTrackingMessageAsync(context, cancellationToken);
        return Result.Merge(result, feedbackResult);
    }

    private async Task<Result> ExecuteAsync(
        (IStep Step, StepInfo Info) data, 
        IOperationContext context,
        CancellationToken cancellationToken)
    {
        var result = await SafeCall(() => data.Step.ExecuteAsync(cancellationToken));
        data.Info.UpdateStatus(result);

        var feedbackResult = await publishMessageService.UpdateTrackingMessageAsync(context, cancellationToken);
        return Result.Merge(result, feedbackResult);
    }

    private static async Task<Result> SafeCall(Func<Task<Result>> func)
    {
        try { return await func(); }
        catch (Exception ex)
        {
            const string ERROR_MESSAGE = "Fatal error ocurred. More information inside log file.";
            Log.Error(ex, ERROR_MESSAGE);

            return Result.Fail(new Error(ERROR_MESSAGE).CausedBy(ex));
        }
    }
}
