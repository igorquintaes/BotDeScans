namespace BotDeScans.App.Models;

public record TitleReference
{
    public required int Id { get; init; }
    public required ExternalReference Key { get; init; }
    public required string Value { get; init; }
}

public enum ExternalReference
{
    MangaDex,
    TsukiMangas
}
