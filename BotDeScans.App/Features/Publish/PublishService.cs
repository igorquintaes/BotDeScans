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
    public virtual Task<Result> ValidateBeforeFilesManagementAsync(
        InteractionContext interactionContext, 
        CancellationToken cancellationToken)
        => RunStepsAsync(
            interactionContext,
            stepFunc: async (step, ct) => await step.ValidateBeforeFilesManagementAsync(ct),
            stepTypes: Enum.GetValues<StepType>(),
            isPublishing: false,
            cancellationToken: cancellationToken);

    public virtual Task<Result> ValidateAfterFilesManagementAsync(
        InteractionContext interactionContext, 
        CancellationToken cancellationToken)
        => RunStepsAsync(
            interactionContext,
            stepFunc: async (step, ct) => await step.ValidateAfterFilesManagementAsync(ct),
            stepTypes: Enum.GetValues<StepType>(),
            isPublishing: false,
            cancellationToken: cancellationToken);

    public virtual Task<Result> RunManagementStepsAsync(
        InteractionContext interactionContext,
        CancellationToken cancellationToken)
        => RunStepsAsync(
            interactionContext,
            stepFunc: async (step, ct) => await step.ExecuteAsync(ct),
            stepTypes: [StepType.Management],
            isPublishing: true,
            cancellationToken: cancellationToken);

    public virtual Task<Result> RunPublishStepsAsync(
        InteractionContext interactionContext,
        CancellationToken cancellationToken)
        => RunStepsAsync(
            interactionContext,
            stepFunc: async (step, ct) => await step.ExecuteAsync(ct),
            stepTypes: [StepType.Publish],
            isPublishing: true,
            cancellationToken: cancellationToken);

    private async Task<Result> RunStepsAsync(
        InteractionContext interactionContext,
        Func<IStep, CancellationToken, Task<Result>> stepFunc,
        StepType[] stepTypes,
        bool isPublishing,
        CancellationToken cancellationToken)
    {
        var result = new Result();
        foreach (var step in steps
            .Where(x => publishState.Steps.Value[x.StepName] != StepStatus.Skip)
            .Where(x => stepTypes.Contains(x.StepType))
            .OrderBy(x => x.StepName))
        {
            if (isPublishing)
                publishState.Steps.Value[step.StepName] = StepStatus.Executing;

            try
            {
                var executionResult = await stepFunc(step, cancellationToken);
                result.WithReasons(executionResult.Reasons);

                if (executionResult.IsFailed)
                    publishState.Steps.Value[step.StepName] = StepStatus.Error;
                else if (isPublishing)
                    publishState.Steps.Value[step.StepName] = StepStatus.Success;

                if (isPublishing)
                {
                    var initialFeedbackResult = await publishMessageService.UpdateTrackingMessageAsync(interactionContext, cancellationToken);
                    result.WithReasons(initialFeedbackResult.Reasons);

                    if (result.IsFailed)
                        return result;
                }
            }
            catch (Exception ex)
            {
                var message = $"Unexpected error in {step.StepName}. " +
                              $"Exception message: {ex.Message}. " +
                               "More info inside logs file.";

                Log.Error(ex, message);
                result.WithError(new Error(message).CausedBy(ex));

                publishState.Steps.Value[step.StepName] = StepStatus.Fatal;
                return result;
            }
        }

        return result;
    }
}

public enum PingType
{
    Everyone = 1,
    Global = 2,
    Role = 3,
    None = 4
}