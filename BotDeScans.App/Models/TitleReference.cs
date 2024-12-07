using System.ComponentModel;
namespace BotDeScans.App.Models;

public record TitleReference
{
    public int Id { get; init; }
    public int TitleId { get; init; }
    public required Title Title { get; init; } = null!;
    public required ExternalReference Key { get; init; }
    public required string Value { get; init; }
}

public enum ExternalReference
{
    [Description("MangaDex")]
    MangaDex
}
