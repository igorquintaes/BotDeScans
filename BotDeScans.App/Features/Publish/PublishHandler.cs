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

        var publishPingResult = await publishService.CreatePingMessageAsync(publishState.Info.DisplayTitle, cancellationToken);
        if (publishPingResult.IsFailed)
            return publishPingResult;

        var startingFeedbackResult = await feedbackFunc();
        if (startingFeedbackResult.IsFailed)
            return startingFeedbackResult;

        var managePublishResult = await publishService.RunAsync(StepType.Manage, feedbackFunc, cancellationToken);
        if (managePublishResult.IsFailed)
            return managePublishResult;

        var validationResult = await publishService.ValidateAfterFilesManagementAsync(cancellationToken);
        if (validationResult.IsFailed)
            return validationResult;

        return await publishService.RunAsync(StepType.Execute, feedbackFunc, cancellationToken);
    }
}
