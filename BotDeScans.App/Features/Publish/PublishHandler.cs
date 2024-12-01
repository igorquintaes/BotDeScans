using FluentResults;
namespace BotDeScans.App.Features.Publish;

public class PublishHandler(PublishService publishService)
{
    public async Task<Result<string>> HandleAsync(
        Func<Task<Result>> feedbackFunc,
        CancellationToken cancellationToken)
    {
        var preValidationResult = await publishService.ValidateBeforeFilesManagementAsync(cancellationToken);
        if (preValidationResult.IsFailed)
            return preValidationResult;

        var pingResult = await publishService.CreatePingMessageAsync(cancellationToken);
        if (pingResult.IsFailed)
            return pingResult;

        var initialFeedbackResult = await feedbackFunc();
        if (initialFeedbackResult.IsFailed)
            return initialFeedbackResult;

        var managementResult = await publishService.RunManagementStepsAsync(feedbackFunc, cancellationToken);
        if (managementResult.IsFailed)
            return managementResult;

        var validationResult = await publishService.ValidateAfterFilesManagementAsync(cancellationToken);
        if (validationResult.IsFailed)
            return validationResult;

        var publishResult = await publishService.RunPublishStepsAsync(feedbackFunc, cancellationToken);
        if (publishResult.IsFailed)
            return publishResult;

        return pingResult;
    }
}
