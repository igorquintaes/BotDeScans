using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using FluentResults;
using Remora.Discord.Commands.Contexts;
using Serilog;
namespace BotDeScans.App.Features.Publish;

public class PublishService(
    PublishMessageService publishMessageService,
    PublishState publishState,
    IEnumerable<IStep> steps)
{
    public virtual Task<Result> ValidateBeforeFilesManagementAsync(
        InteractionContext interactionContext,
        CancellationToken cancellationToken) =>
        RunAsync(
            interactionContext,
            stepFunc: async (step, ct) => await step.ValidateAfterFilesManagementAsync(ct),
            steps: steps,
            cancellationToken: cancellationToken);

    public virtual Task<Result> ValidateAfterFilesManagementAsync(
        InteractionContext interactionContext,
        CancellationToken cancellationToken) => 
        RunAsync(
            interactionContext,
            stepFunc: async (step, ct) => await step.ValidateAfterFilesManagementAsync(ct),
            steps: steps,
            cancellationToken: cancellationToken);

    public virtual async Task<Result> ExecuteStepsAsync(
        InteractionContext interactionContext,
        StepType stepType,
        CancellationToken cancellationToken)
    {
        foreach (var step in steps
            .Where(x => x.StepType == stepType)
            .OrderBy(x => x))
        {
            publishState.Steps![step.StepName] = StepStatus.Executing;

            var result = await RunAsync(
                interactionContext,
                stepFunc: async (step, ct) => await step.ValidateAfterFilesManagementAsync(ct),
                steps: [step],
                cancellationToken: cancellationToken);

            publishState.Steps![step.StepName] = result.IsSuccess 
                ? StepStatus.Success 
                : StepStatus.Error;

            if (result.IsFailed)
                return result;
        }

        return Result.Ok();
    }

    private async Task<Result> RunAsync(
        InteractionContext interactionContext,
        Func<IStep, CancellationToken, Task<Result>> stepFunc,
        IEnumerable<IStep> steps,
        CancellationToken cancellationToken)
    {
        foreach (var step in steps.OrderBy(x => x))
        {
            try
            {
                var executionResult = await stepFunc(step, cancellationToken);
                if (executionResult.IsSuccess)
                    continue;

                return await UpdateTrackingMessageOnErrorAsync(interactionContext, executionResult, cancellationToken);
            }
            catch (Exception ex)
            {
                var message = $"Unexpected error while executing {nameof(RunAsync)} for {step.StepName}";
                Log.Error(ex, message);

                var exceptionResult = Result.Fail(new Error(message).CausedBy(ex));
                return await UpdateTrackingMessageOnErrorAsync(interactionContext, exceptionResult, cancellationToken);
            }
        }

        return Result.Ok();
    }

    private async Task<Result> UpdateTrackingMessageOnErrorAsync(
        InteractionContext interactionContext, 
        Result runResult,
        CancellationToken cancellationToken)
    {
        var errorMessages = runResult.Errors.Select(x => x.Message);
        Log.Error(string.Join(Environment.NewLine, errorMessages));

        var initialFeedbackResult = await publishMessageService.UpdateTrackingMessageAsync(interactionContext, cancellationToken);
        if (initialFeedbackResult.IsFailed)
            return Result.Merge(runResult, initialFeedbackResult);

        return runResult;
    }
}
