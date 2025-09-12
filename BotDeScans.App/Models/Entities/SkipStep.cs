using BotDeScans.App.Models.Entities.Enums;

namespace BotDeScans.App.Models.Entities;

public class SkipStep
{
    public int Id { get; init; }
    public required StepName Step { get; init; }

    public int TitleId { get; init; }
    public Title Title { get; init; } = null!;
}
