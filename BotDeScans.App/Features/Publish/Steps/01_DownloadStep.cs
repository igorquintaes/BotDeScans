using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Services;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
namespace BotDeScans.App.Features.Publish.Steps;

public class DownloadStep(
    IServiceProvider serviceProvider,
    GoogleDriveService googleDriveService,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.Download;
    public StepType StepType => StepType.Management;

    public async Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken cancellationToken)
    {
        var folderIdResult = googleDriveService.GetFolderIdFromUrl(state.ReleaseInfo.DownloadUrl);
        if (folderIdResult.IsFailed)
            return folderIdResult.ToResult();

        state.InternalData.GoogleDriveFolderId = folderIdResult.Value;
        return await googleDriveService.ValidateFilesAsync(folderIdResult.Value, cancellationToken);
    }

    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var googleDriveService = serviceProvider.GetRequiredService<GoogleDriveService>();
        var fileReleaseService = serviceProvider.GetRequiredService<FileReleaseService>();

        state.InternalData.OriginContentFolder = fileReleaseService.CreateScopedDirectory();

        var saveFilesResult = await googleDriveService.SaveFilesAsync(
            state.InternalData.GoogleDriveFolderId,
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
