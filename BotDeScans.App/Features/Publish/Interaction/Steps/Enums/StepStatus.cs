using BotDeScans.App.Attributes;

namespace BotDeScans.App.Features.Publish.Interaction.Steps.Enums;

public enum StepStatus
{
    [Emoji("clock9")]
    QueuedForValidation,
    [Emoji("clock10")]
    QueuedForExecution,
    [Emoji("white_check_mark")]
    Success,
    [Emoji("warning")]
    Error
}
