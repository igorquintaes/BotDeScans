using BotDeScans.App.Features.Mega;
using BotDeScans.App.Features.Mega.InternalServices;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public class UploadPdfMegaStep(
MegaService megaService,
MegaSettingsService megaSettingsService,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.UploadPdfMega;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
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
