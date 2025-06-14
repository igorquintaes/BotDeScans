﻿using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class DownloadStep(
    FileReleaseService fileReleaseService,
    GoogleDriveService googleDriveService,
    State state) : IManagementStep
{
    public StepType Type => StepType.Management;
    public StepName Name => StepName.Download;
    public bool IsMandatory => true;

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var downloadDirectory = fileReleaseService.CreateScopedDirectory();
        var coverDirectory = fileReleaseService.CreateScopedDirectory();

        var saveFilesResult = await googleDriveService.SaveFilesAsync(
            state.ChapterInfo.GoogleDriveUrl.Id,
            downloadDirectory,
            cancellationToken);

        if (saveFilesResult.IsFailed)
            return saveFilesResult;

        state.InternalData.OriginContentFolder = downloadDirectory;
        state.InternalData.CoverFilePath = fileReleaseService.MoveCoverFile(
            downloadDirectory,
            coverDirectory);

        return Result.Ok();
    }
}
