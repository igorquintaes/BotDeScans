using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using FluentResults;
namespace BotDeScans.App.Features.Publish.State.Models;

public class StepInfo(IStep step)
{
    public StepStatus StepStatus { get; private set; } = step.Type == StepType.Management
         ? StepStatus.QueuedForExecution
         : StepStatus.QueuedForValidation;

    public void UpdateStatus(Result result) =>
        StepStatus = result.IsFailed
            ? StepStatus.Error
            : StepStatus == StepStatus.QueuedForValidation
                ? StepStatus.QueuedForExecution
                : StepStatus.Success;
}
