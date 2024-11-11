using BotDeScans.App.Attributes;
using FluentResults;
using System.ComponentModel;

namespace BotDeScans.App.Features.Publish.Steps;

public interface IStep
{
    public Task<Result> ExecuteAsync(CancellationToken cancellationToken);
    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken cancellationToken);
    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken cancellationToken);
    public StepEnum StepName { get; }
    public StepType StepType { get; }
}

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
