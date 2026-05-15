using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Models.Entities;

namespace BotDeScans.App.Features.Publish.Interaction;

public interface IPublishContext
{
    Title Title { get; }
    Info ChapterInfo { get; }

    string OriginContentFolder { get; }
    string CoverFilePath { get; }
    string? ZipFilePath { get; }
    string? PdfFilePath { get; }
    string? BloggerImageAsBase64 { get; }
    string? BoxPdfReaderKey { get; }
    string? Pings { get; }

    string? MegaZipLink { get; }
    string? MegaPdfLink { get; }
    string? DriveZipLink { get; }
    string? DrivePdfLink { get; }
    string? BoxZipLink { get; }
    string? BoxPdfLink { get; }
    string? MangaDexLink { get; }
    string? SakuraMangasLink { get; }
    string? BloggerLink { get; }

    void SetOriginContentFolder(string originContentFolder);
    void SetCoverFilePath(string coverFilePath);
    void SetZipPath(string zipFilePath);
    void SetPdfPath(string pdfFilePath);
    void SetBloggerImageAsBase64(string bloggerImageAsBase64);
    void SetBoxPdfReaderKey(string boxPdfReaderKey);
    void SetPings(string pings);

    void SetMegaZipLink(string link);
    void SetMegaPdfLink(string link);
    void SetDriveZipLink(string link);
    void SetDrivePdfLink(string link);
    void SetBoxZipLink(string link);
    void SetBoxPdfLink(string link);
    void SetMangaDexLink(string link);
    void SetSakuraMangasLink(string link);
    void SetBloggerLink(string link);
}
