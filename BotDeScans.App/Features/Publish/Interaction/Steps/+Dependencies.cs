﻿using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddPublishSteps(this IServiceCollection services) => services
        .AddScoped<IStep, PublishBloggerStep>()
        .AddScoped<IStep, UploadSakuraMangasStep>()
        .AddScoped<IStep, UploadMangaDexStep>()
        .AddScoped<IStep, UploadPdfBoxStep>()
        .AddScoped<IStep, UploadZipBoxStep>()
        .AddScoped<IStep, UploadPdfGoogleDriveStep>()
        .AddScoped<IStep, UploadZipGoogleDriveStep>()
        .AddScoped<IStep, UploadPdfMegaStep>()
        .AddScoped<IStep, UploadZipMegaStep>()
        .AddScoped<IStep, PdfFilesStep>()
        .AddScoped<IStep, ZipFilesStep>()
        .AddScoped<IStep, CompressFilesStep>()
        .AddScoped<IStep, DownloadStep>()
        .AddScoped<IStep, SetupStep>()
        .AddScoped<StepsService>();
}
