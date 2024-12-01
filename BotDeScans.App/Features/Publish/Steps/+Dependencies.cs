using Microsoft.Extensions.DependencyInjection;
namespace BotDeScans.App.Features.Publish.Steps;

internal static class AddDependencies
{
    internal static IServiceCollection AddPublishSteps(this IServiceCollection services) => services
        .AddScoped<IStep, PublishBloggerStep>()
        .AddScoped<IStep, PublishMangadexStep>()
        .AddScoped<IStep, UploadPdfBoxStep>()
        .AddScoped<IStep, UploadZipBoxStep>()
        .AddScoped<IStep, UploadPdfGoogleDriveStep>()
        .AddScoped<IStep, UploadZipGoogleDriveStep>()
        .AddScoped<IStep, UploadPdfMegaStep>()
        .AddScoped<IStep, UploadZipMegaStep>()
        .AddScoped<IStep, PdfFilesStep>()
        .AddScoped<IStep, ZipFilesStep>()
        .AddScoped<IStep, CompressFilesStep>()
        .AddScoped<IStep, DownloadStep>();
}
