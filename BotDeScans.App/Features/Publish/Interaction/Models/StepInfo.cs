using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Models;

public record StepInfo
{
    public virtual StepStatus Status { get; init; }

    public StepInfo(IStep step, bool skip = false)
    {
        Status = skip
            ? StepStatus.Skip
            : step is IManagementStep
                ? StepStatus.QueuedForExecution
                : StepStatus.QueuedForValidation;
    }

    public virtual StepInfo UpdateStatus(Result result) =>
        this with
        {
            Status = result.IsFailed
                ? StepStatus.Error
                : Status == StepStatus.QueuedForValidation
                    ? StepStatus.QueuedForExecution
                    : StepStatus.Success
        };
}
