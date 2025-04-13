using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.Steps;
using FluentResults;
using Remora.Discord.Commands.Contexts;
using Serilog;
namespace BotDeScans.App.Features.Publish;

public class PublishService(
    PublishMessageService publishMessageService,
    PublishState publishState,
    IEnumerable<IStep> steps)
{
    public virtual async Task<Result> ValidateBeforeFilesManagementAsync(
        InteractionContext interactionContext,
        CancellationToken cancellationToken)
    {
        foreach (var step in steps.OrderBy(x => x.StepName))
        {
            var result = await RunAsync(
                stepFunc: async (step, ct) => await step.ValidateBeforeFilesManagementAsync(ct),
                step: step,
                cancellationToken: cancellationToken);

            if (result.IsFailed)
            {
                var initialFeedbackResult = await publishMessageService.UpdateTrackingMessageAsync(interactionContext, cancellationToken);
                if (initialFeedbackResult.IsFailed)
                    return initialFeedbackResult;

                return result.ToResult();
            }
        }

        return Result.Ok();
    }

    public virtual async Task<Result> ValidateAfterFilesManagementAsync(
        InteractionContext interactionContext,
        CancellationToken cancellationToken)
    {
        foreach (var step in steps.OrderBy(x => x.StepName))
        {
            var result = await RunAsync(
                stepFunc: async (step, ct) => await step.ValidateAfterFilesManagementAsync(ct),
                step: step,
                cancellationToken: cancellationToken);

            if (result.IsFailed)
            {
                var initialFeedbackResult = await publishMessageService.UpdateTrackingMessageAsync(interactionContext, cancellationToken);
                if (initialFeedbackResult.IsFailed)
                    return initialFeedbackResult;

                return result.ToResult();
            }
        }

        return Result.Ok();
    }

    public virtual async Task<Result> ExecuteStepsAsync(
        InteractionContext interactionContext,
        StepType stepType,
        CancellationToken cancellationToken)
    {
        var validSteps = steps
            .Where(x => x.StepType == stepType)
            .OrderBy(x => x.StepName);

        foreach (var step in validSteps)
        {
            publishState.Steps![step.StepName] = StepStatus.Executing;

            var result = await RunAsync(
                stepFunc: async (step, ct) => await step.ValidateAfterFilesManagementAsync(ct),
                step: step,
                cancellationToken: cancellationToken);

            publishState.Steps![step.StepName] = result.ValueOrDefault;

            var initialFeedbackResult = await publishMessageService.UpdateTrackingMessageAsync(interactionContext, cancellationToken);
            if (initialFeedbackResult.IsFailed)
                return initialFeedbackResult;

            if (result.IsFailed)
                return result.ToResult();
        }

        return Result.Ok();
    }

    private static async Task<Result<StepStatus>> RunAsync(
        Func<IStep, CancellationToken, Task<Result>> stepFunc,
        IStep step,
        CancellationToken cancellationToken)
    {
        try
        {
            var executionResult = await stepFunc(step, cancellationToken);
            return executionResult.IsFailed
                ? Result.Fail<StepStatus>(executionResult.Errors).WithValue(StepStatus.Error)
                : Result.Ok(StepStatus.Success);

        }
        catch (Exception ex)
        {
            var message = $"Unexpected error in {step.StepName}. " +
                          $"Exception message: {ex.Message}. " +
                           "More info inside logs file.";

            Log.Error(ex, message);
            return Result.Fail<StepStatus>(message).WithValue(StepStatus.Fatal);
        }
    }
}

public enum PingType
{
    Everyone = 1,
    Global = 2,
    Role = 3,
    None = 4
}