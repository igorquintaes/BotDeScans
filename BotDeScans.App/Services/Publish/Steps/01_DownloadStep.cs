﻿using BotDeScans.App.Enums;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
namespace BotDeScans.App.Services.Publish.Steps;

public class DownloadStep(
    IServiceProvider serviceProvider,
    GoogleDriveService googleDriveService,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.Download;
    public StepType StepType => StepType.Management;

    public async Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken cancellationToken)
        => await googleDriveService.ValidateFilesFromLinkAsync(state.Info.Link, cancellationToken);

    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _) 
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var googleDriveService = serviceProvider.GetRequiredService<GoogleDriveService>();
        var fileReleaseService = serviceProvider.GetRequiredService<FileReleaseService>();

        state.InternalData.OriginContentFolder = fileReleaseService.CreateScopedDirectory();

        var saveFilesResult = await googleDriveService.SaveFilesFromLinkAsync(
            state.Info.Link,
            state.InternalData.OriginContentFolder,
            cancellationToken);

        if (saveFilesResult.IsFailed)
            return saveFilesResult;

        state.InternalData.CoverFilePath = fileReleaseService.MoveCoverFile(
            state.InternalData.OriginContentFolder,
            fileReleaseService.CreateScopedDirectory());

        return Result.Ok();
    }
}
