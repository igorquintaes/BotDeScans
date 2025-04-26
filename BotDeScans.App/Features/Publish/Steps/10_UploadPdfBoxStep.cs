using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public class UploadPdfBoxStep(
    BoxService boxService,
    PublishState state) : PublishStep
{
    public override StepType Type => StepType.Upload;
    public override StepName Name => StepName.UploadPdfBox;

    public override Task<Result> ValidateAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public override async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var titleFolder = await boxService.GetOrCreateFolderAsync(state.Title.Name);
        var file = await boxService.CreateFileAsync(
            filePath: state.InternalData.PdfFilePath!,
            parentFolderId: titleFolder.Id);

        state.ReleaseLinks.BoxPdf = file.SharedLink.DownloadUrl;
        state.ReleaseLinks.BoxPdfReaderKey = file.SharedLink.DownloadUrl
            .Split("/")
            .Last()
            .Replace(".pdf", "", StringComparison.InvariantCultureIgnoreCase);

        return Result.Ok();
    }
}
