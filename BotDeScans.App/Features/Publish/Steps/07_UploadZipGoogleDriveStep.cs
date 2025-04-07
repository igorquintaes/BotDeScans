using BotDeScans.App.Features.GoogleDrive;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public class UploadZipGoogleDriveStep(
    GoogleDriveService googleDriveService,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.UploadZipGoogleDrive;
    public StepType StepType => StepType.Publish;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok()); 
    // todo: pegar o tamanho do arquivo e ver se tem espaço disponível no Google Drive

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var titleFolderResult = await googleDriveService.GetOrCreateFolderAsync(state.Title.Name, default, cancellationToken);
        if (titleFolderResult.IsFailed)
            return titleFolderResult.ToResult();

        var fileResult = await googleDriveService.CreateFileAsync(
            filePath: state.InternalData.ZipFilePath!,
            parentId: titleFolderResult.Value.Id,
            publicAccess: true,
            cancellationToken);

        if (fileResult.IsFailed)
            return fileResult.ToResult();

        state.ReleaseLinks.DriveZip = fileResult.Value.WebViewLink;
        return Result.Ok();
    }
}
