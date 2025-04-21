using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using FluentResults;
using Microsoft.EntityFrameworkCore.Storage;
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
            stepFunc: async (step, ct) => await step.ValidateBeforeFilesManagementAsync(ct),
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
            .Where(x => publishState.Steps.Any(y => y.Key == x.StepName))
            .OrderBy(x => x.StepName))
        {
            publishState.Steps![step.StepName] = StepStatus.Executing;

            var runResult = await RunAsync(
                interactionContext,
                stepFunc: async (step, ct) => await step.ExecuteAsync(ct),
                steps: [step],
                cancellationToken: cancellationToken);

            publishState.Steps![step.StepName] = runResult.IsSuccess
                ? StepStatus.Success
            : StepStatus.Error;

            var uppateTrackingMessageResult = await UpdateTrackingMessageOnErrorAsync(
                interactionContext,
                runResult, 
                cancellationToken);

            if (uppateTrackingMessageResult.IsFailed)
                return runResult;
        }

        return Result.Ok();
    }

    private async Task<Result> RunAsync(
        InteractionContext interactionContext,
        Func<IStep, CancellationToken, Task<Result>> stepFunc,
        IEnumerable<IStep> steps,
        CancellationToken cancellationToken)
    {
        try
        {
            foreach (var step in steps
                .Where(x => publishState.Steps.Any(y => y.Key == x.StepName))
                .OrderBy(x => x.StepName))
            {
                var executionResult = await stepFunc(step, cancellationToken);
                if (executionResult.IsSuccess)
                    continue;

                return await UpdateTrackingMessageOnErrorAsync(interactionContext, executionResult, cancellationToken);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            var message = $"Unexpected error while executing {nameof(RunAsync)}. Info inside logs.";
            Log.Error(ex, message);

            var exceptionResult = Result.Fail(new Error(message).CausedBy(ex));
            return await UpdateTrackingMessageOnErrorAsync(interactionContext, exceptionResult, cancellationToken);
        }
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
