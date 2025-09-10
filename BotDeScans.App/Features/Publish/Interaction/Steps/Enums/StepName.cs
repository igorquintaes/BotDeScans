using System.ComponentModel;

namespace BotDeScans.App.Features.Publish.Interaction.Steps.Enums;

public enum StepName
{
    [Description("Inicializar")]
    Setup = 1,
    [Description("Baixar")]
    Download = 2,
    [Description("Compressão")]
    Compress = 3,
    [Description("Transformar em zip")]
    ZipFiles = 4,
    [Description("Transformar em pdf")]
    PdfFiles = 5,
    [Description("Hospedar zip - Mega")]
    UploadZipMega = 6,
    [Description("Hospedar pdf - Mega")]
    UploadPdfMega = 7,
    [Description("Hospedar zip - Box")]
    UploadZipBox = 8,
    [Description("Hospedar pdf - Box")]
    UploadPdfBox = 9,
    [Description("Hospedar zip - Google Drive")]
    UploadZipGoogleDrive = 10,
    [Description("Hospedar pdf - Google Drive")]
    UploadPdfGoogleDrive = 11,
    [Description("Publicar na Mangadex")]
    UploadMangadex = 12,
    [Description("Publicar na Sakura Mangás")]
    UploadSakuraMangas = 13,
    [Description("Publicar na Blogspot")]
    PublishBlogspot = 14
}