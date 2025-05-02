using BotDeScans.App.Features.Publish.State;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public class UploadZipBoxStep(
    BoxService boxService,
    PublishState state) : IPublishStep
{
    public StepType Type => StepType.Upload;
    public StepName Name => StepName.UploadZipBox;
    public StepName? Dependency => StepName.ZipFiles;

    public Task<Result> ValidateAsync(CancellationToken _)
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
