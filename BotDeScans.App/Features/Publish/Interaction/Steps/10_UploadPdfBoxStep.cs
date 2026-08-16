using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class UploadPdfBoxStep(
    BoxService boxService) : IPublishStep
{
    public StepType Type => StepType.Upload;
    public StepName Name => StepName.UploadPdfBox;
    public StepName? Dependency => StepName.PdfFiles;

    public Task<Result> ValidateAsync(State state, CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result<State>> ExecuteAsync(State state, CancellationToken cancellationToken)
    {
        var titleFolderResult = await boxService.GetOrCreateFolderAsync(state.Title.Name, cancellationToken);
        if (titleFolderResult.IsFailed)
            return titleFolderResult.ToResult();

        var fileResult = await boxService.CreateFileAsync(
            filePath: state.PdfFilePath!,
            parentFolderId: titleFolderResult.Value.Id,
            cancellationToken: cancellationToken);

        var updatedState = state with
        {
            BoxPdfLink = fileResult.ValueOrDefault?.SharedLink!.DownloadUrl!,
            BoxPdfReaderKey = fileResult.ValueOrDefault?.SharedLink!.DownloadUrl!
                .Split("/").Last().Replace(".pdf", "", StringComparison.InvariantCultureIgnoreCase)
        };

        return fileResult.Map(updatedState);
    }
}
