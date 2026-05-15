using BotDeScans.App.Features.Mega;
using BotDeScans.App.Features.Mega.InternalServices;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class UploadPdfMegaStep(
    MegaService megaService,
    MegaSettingsService megaSettingsService) : IPublishStep
{
    public StepType Type => StepType.Upload;
    public StepName Name => StepName.UploadPdfMega;
    public StepName? Dependency => StepName.PdfFiles;

    public Task<Result> ValidateAsync(State state, CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result<State>> ExecuteAsync(State state, CancellationToken cancellationToken)
    {
        var root = await megaSettingsService.GetRootFolderAsync();
        var titleFolder = await megaService.GetOrCreateFolderAsync(state.Title.Name, root);
        if (titleFolder.IsFailed)
            return titleFolder.ToResult<State>();

        var fileResult = await megaService.CreateFileAsync(
            filePath: state.PdfFilePath!,
            parentNode: titleFolder.Value,
            cancellationToken);

        if (fileResult.IsFailed)
            return fileResult.ToResult<State>();

        return Result.Ok(state.WithMegaPdfLink(fileResult.Value.AbsoluteUri));
    }
}
