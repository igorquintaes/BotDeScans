using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class UploadPdfBoxStep(
    BoxService boxService,
    IPublishContext context) : IPublishStep
{
    public StepType Type => StepType.Upload;
    public StepName Name => StepName.UploadPdfBox;
    public StepName? Dependency => StepName.PdfFiles;

    public Task<Result> ValidateAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var titleFolder = await boxService.GetOrCreateFolderAsync(context.Title.Name, cancellationToken);
        var file = await boxService.CreateFileAsync(
            filePath: context.PdfFilePath!,
            parentFolderId: titleFolder.Id,
            cancellationToken: cancellationToken);

        context.SetBoxPdfLink(file.SharedLink!.DownloadUrl);
        context.SetBoxPdfReaderKey(file.SharedLink.DownloadUrl!
            .Split("/")
            .Last()
            .Replace(".pdf", "", StringComparison.InvariantCultureIgnoreCase));

        return Result.Ok();
    }
}
