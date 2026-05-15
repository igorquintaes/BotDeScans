namespace BotDeScans.App.Features.Publish.Interaction.Models;

public record InternalData
{
    public string OriginContentFolder { get; init; } = null!;
    public string CoverFilePath { get; init; } = null!;
    public string? ZipFilePath { get; init; }
    public string? PdfFilePath { get; init; }
    public string? BloggerImageAsBase64 { get; init; }
    public string? BoxPdfReaderKey { get; init; }
    public string? Pings { get; init; }
    public TrackingMessage? TrackingMessage { get; init; }
}