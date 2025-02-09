using BotDeScans.App.Services;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public class PdfFilesStep(
    FileService fileService,
    FileReleaseService fileReleaseService,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.PdfFiles;
    public StepType StepType => StepType.Management;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var pdfFileResult = await fileService.CreatePdfFileAsync(
            fileName: state.ReleaseInfo.ChapterNumber,
            resourcesDirectory: state.InternalData.OriginContentFolder,
            destinationDirectory: fileReleaseService.CreateScopedDirectory());

        if (pdfFileResult.IsFailed)
            return pdfFileResult.ToResult();

        state.InternalData.PdfFilePath = pdfFileResult.Value;
        return Result.Ok();
    }
}
