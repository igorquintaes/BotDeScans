using BotDeScans.App.Features.Mega;
using BotDeScans.App.Features.Mega.InternalServices;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class UploadPdfMegaStep(
    MegaService megaService,
    MegaSettingsService megaSettingsService,
    State state) : IPublishStep
{
    public StepType Type => StepType.Upload;
    public StepName Name => StepName.UploadPdfMega;
    public StepName? Dependency => StepName.PdfFiles;

    public Task<Result> ValidateAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var root = await megaSettingsService.GetRootFolderAsync();
        var titleFolder = await megaService.GetOrCreateFolderAsync(state.Title.Name, root);
        if (titleFolder.IsFailed)
            return titleFolder.ToResult();

        var fileResult = await megaService.CreateFileAsync(
            filePath: state.InternalData.PdfFilePath!,
            parentNode: titleFolder.Value,
            cancellationToken);

        if (fileResult.IsFailed)
            return fileResult.ToResult();

        state.ReleaseLinks.MegaPdf = fileResult.Value.AbsoluteUri;
        return Result.Ok();
    }
}
