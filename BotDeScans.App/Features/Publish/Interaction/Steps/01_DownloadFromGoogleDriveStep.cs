using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class DownloadFromGoogleDriveStep(
    FileReleaseService fileReleaseService,
    GoogleDriveService googleDriveService) : IManagementStep
{
    public StepType Type => StepType.Management;
    public StepName Name => StepName.Download;
    public bool IsMandatory => true;

    public async Task<Result<State>> ExecuteAsync(State state, CancellationToken cancellationToken)
    {
        var downloadDirectory = fileReleaseService.CreateScopedDirectory();
        var coverDirectory = fileReleaseService.CreateScopedDirectory();

        var saveFilesResult = await googleDriveService.SaveFilesAsync(
            state.ChapterInfo.GoogleDriveUrl.Id,
            downloadDirectory,
            cancellationToken);

        if (saveFilesResult.IsFailed)
            return saveFilesResult.ToResult<State>();

        var updatedState = state
            .WithOriginContentFolder(downloadDirectory)
            .WithCoverFilePath(fileReleaseService.MoveCoverFile(downloadDirectory, coverDirectory));

        return Result.Ok(updatedState);
    }
}
