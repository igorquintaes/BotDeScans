using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class UploadZipBoxStep(
    BoxService boxService,
    State state) : IPublishStep
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
