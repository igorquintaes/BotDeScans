using BotDeScans.App.Attributes;
namespace BotDeScans.App.Features.Publish.Steps.Enums;

public enum StepStatus
{
    [Emoji("clock10")]
    Queued,
    [Emoji("fire")]
    Executing,
    [Emoji("white_check_mark")]
    Success,
    [Emoji("warning")]
    Error
}
