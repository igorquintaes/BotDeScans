using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using FluentResults;
namespace BotDeScans.App.Features.Publish.State.Models;

public class StepInfo(IStep step)
{
    public StepStatus Status { get; private set; } = step is IManagementStep
         ? StepStatus.QueuedForExecution
         : StepStatus.QueuedForValidation;

    public virtual void UpdateStatus(Result result) =>
        Status = result.IsFailed
            ? StepStatus.Error
            : Status == StepStatus.QueuedForValidation
                ? StepStatus.QueuedForExecution
                : StepStatus.Success;
}
