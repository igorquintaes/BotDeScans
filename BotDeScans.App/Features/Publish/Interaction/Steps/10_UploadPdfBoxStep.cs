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
        var titleFolder = await boxService.GetOrCreateFolderAsync(state.Title.Name, cancellationToken);
        var file = await boxService.CreateFileAsync(
            filePath: state.PdfFilePath!,
            parentFolderId: titleFolder.Id,
            cancellationToken: cancellationToken);

        var updatedState = state with
        {
            BoxPdfLink = file.SharedLink!.DownloadUrl!,
            BoxPdfReaderKey = file.SharedLink!.DownloadUrl!
                .Split("/")
                .Last()
                .Replace(".pdf", "", StringComparison.InvariantCultureIgnoreCase)
        };

        return Result.Ok(updatedState);
    }
}
