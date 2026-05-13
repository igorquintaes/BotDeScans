using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class UploadZipBoxStep(
    BoxService boxService,
    IPublishContext context) : IPublishStep
{
    public StepType Type => StepType.Upload;
    public StepName Name => StepName.UploadZipBox;
    public StepName? Dependency => StepName.ZipFiles;

    public Task<Result> ValidateAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var titleFolder = await boxService.GetOrCreateFolderAsync(context.Title.Name, cancellationToken);
        var file = await boxService.CreateFileAsync(
            filePath: context.ZipFilePath!,
            parentFolderId: titleFolder.Id,
            cancellationToken: cancellationToken);

        context.SetBoxZipLink(file.SharedLink!.DownloadUrl);
        return Result.Ok();
    }
}
