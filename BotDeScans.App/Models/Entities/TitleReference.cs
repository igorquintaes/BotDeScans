using System.ComponentModel;

namespace BotDeScans.App.Models.Entities;

public class TitleReference
{
    public int Id { get; init; }
    public required ExternalReference Key { get; init; }
    public required string Value { get; set; }

    public int TitleId { get; init; }
    public Title Title { get; init; } = null!;
}

public enum ExternalReference
{
    [Description("MangaDex")]
    MangaDex,
    [Description("Sakura Mangás")]
    SakuraMangas,
}
