namespace BotDeScans.App.Features.Publish.Interaction.Models;

public record InternalData
{
    public string OriginContentFolder { get; set; } = null!;
    public string CoverFilePath { get; set; } = null!;
    public string? ZipFilePath { get; set; }
    public string? PdfFilePath { get; set; }
    public string? BloggerImageAsBase64 { get; set; }
    public string? BoxPdfReaderKey { get; set; }
    public string? Pings { get; set; }
}