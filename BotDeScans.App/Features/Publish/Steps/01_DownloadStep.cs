using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public class DownloadStep(
    FileReleaseService fileReleaseService,
    GoogleDriveService googleDriveService,
    PublishState state) : IStep
{
    public StepName StepName => StepName.Download;

    public async Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken cancellationToken)
    {
        var folderIdResult = googleDriveService.GetFolderIdFromUrl(state.ReleaseInfo.DownloadUrl);
        if (folderIdResult.IsFailed)
            return folderIdResult.ToResult();

        var validationResult = await googleDriveService.ValidateFilesAsync(folderIdResult.Value, cancellationToken);
        if (validationResult.IsFailed)
            return validationResult;

        state.InternalData.GoogleDriveFolderId = folderIdResult.Value;
        return Result.Ok();
    }

    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var downloadDirectory = fileReleaseService.CreateScopedDirectory();
        var coverDirectory = fileReleaseService.CreateScopedDirectory();

        var saveFilesResult = await googleDriveService.SaveFilesAsync(
            state.InternalData.GoogleDriveFolderId,
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
