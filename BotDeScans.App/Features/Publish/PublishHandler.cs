using BotDeScans.App.Features.Publish.Steps;
using FluentResults;

namespace BotDeScans.App.Features.Publish;

public class PublishHandler(PublishState publishState, PublishService publishService)
{
    public async Task<Result<string>> HandleAsync(
        Func<Task<Result>> feedbackFunc,
        CancellationToken cancellationToken)
    {
        var preValidationResult = await publishService.ValidateBeforeFilesManagementAsync(cancellationToken);
        if (preValidationResult.IsFailed)
            return preValidationResult;

        var pingResult = await publishService.CreatePingMessageAsync(publishState.Info.DisplayTitle, cancellationToken);
        if (pingResult.IsFailed)
            return pingResult;

        var initialFeedbackResult = await feedbackFunc();
        if (initialFeedbackResult.IsFailed)
            return initialFeedbackResult;

        var managePublishResult = await publishService.RunAsync(StepType.Manage, feedbackFunc, cancellationToken);
        if (managePublishResult.IsFailed)
            return managePublishResult;

        var validationResult = await publishService.ValidateAfterFilesManagementAsync(cancellationToken);
        if (validationResult.IsFailed)
            return validationResult;

        var publishResult = await publishService.RunAsync(StepType.Execute, feedbackFunc, cancellationToken);
        if (publishResult.IsFailed)
            return publishResult;

        return pingResult;
    }
}
