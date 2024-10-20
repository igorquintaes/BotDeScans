using BotDeScans.App.Attributes;
using System.ComponentModel;
namespace BotDeScans.App.Enums;

public enum StepEnum
{
    [Description("Baixar")]
    Download,
    [Description("Compressão")]
    Compress,
    [Description("Transformar em zip")]
    ZipFiles,
    [Description("Transformar em pdf")]
    PdfFiles,
    [Description("Hospedar zip - Mega")]
    UploadZipMega,
    [Description("Hospedar pdf - Mega")]
    UploadPdfMega,
    [Description("Hospedar zip - Box")]
    UploadZipBox,
    [Description("Hospedar pdf - Box")]
    UploadPdfBox,
    [Description("Hospedar zip - Google Drive")]
    UploadZipGoogleDrive,
    [Description("Hospedar pdf - Google Drive")]
    UploadPdfGoogleDrive,
    [Description("Publicar na Mangadex")]
    UploadMangadex,
    [Description("Publicar na Tsuki")]
    UploadTsuki,
    [Description("Publicar no Blogspot")]
    PublishBlogspot
}

public enum StepType
{
    Management,
    Publish
}

public enum StepStatus
{
    [Emoji("track_next")]
    Skip,
    [Emoji("clock10")]
    Queued,
    [Emoji("fire")]
    Executing,
    [Emoji("white_check_mark")]
    Success,
    [Emoji("warning")]
    Error,
}
