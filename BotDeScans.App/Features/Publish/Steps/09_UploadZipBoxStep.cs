using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public class UploadZipBoxStep(
    BoxService boxService,
    PublishState state) : PublishStep
{
    public override StepType Type => StepType.Upload;
    public override StepName Name => StepName.UploadZipBox;

    public override Task<Result> ValidateAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public override async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var titleFolder = await boxService.GetOrCreateFolderAsync(state.Title.Name);
        var file = await boxService.CreateFileAsync(
            filePath: state.InternalData.ZipFilePath!,
            parentFolderId: titleFolder.Id);

        state.ReleaseLinks.BoxZip = file.SharedLink.DownloadUrl;
        return Result.Ok();
    }
}
