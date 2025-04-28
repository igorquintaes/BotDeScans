using System.ComponentModel;
namespace BotDeScans.App.Features.Publish.Steps.Enums;

public enum StepName
{
    [Description("Inicializar")]
    Setup,
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
    [Description("Publicar no Blogspot")]
    PublishBlogspot
}