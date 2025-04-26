using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public class PdfFilesStep(
    FileService fileService,
    FileReleaseService fileReleaseService,
    PublishState state) : IManagementStep
{
    public StepType Type => StepType.Management;
    public StepName Name => StepName.PdfFiles;

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
