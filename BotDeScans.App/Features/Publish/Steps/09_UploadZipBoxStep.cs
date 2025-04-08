using BotDeScans.App.Services;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public class UploadZipBoxStep(
    BoxService boxService,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.UploadZipBox;
    public StepType StepType => StepType.Upload;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var titleFolder = await boxService.GetOrCreateFolderAsync(state.Title.Name);
        var file = await boxService.CreateFileAsync(
            filePath: state.InternalData.ZipFilePath!,
            parentFolderId: titleFolder.Id);

        state.ReleaseLinks.BoxZip = file.SharedLink.DownloadUrl;
        return Result.Ok();
    }
}
