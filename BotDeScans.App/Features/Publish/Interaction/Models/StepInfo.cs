using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Models;

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
