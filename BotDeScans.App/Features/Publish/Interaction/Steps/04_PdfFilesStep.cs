using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class PdfFilesStep(
    FileService fileService,
    FileReleaseService fileReleaseService,
    State state) : IManagementStep
{
    public StepType Type => StepType.Management;
    public StepName Name => StepName.PdfFiles;
    public bool IsMandatory => false;

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var pdfFileResult = await fileService.CreatePdfFileAsync(
            fileName: state.ChapterInfo.ChapterNumber,
            resourcesDirectory: state.InternalData.OriginContentFolder,
            destinationDirectory: fileReleaseService.CreateScopedDirectory());

        if (pdfFileResult.IsFailed)
            return pdfFileResult.ToResult();

        state.InternalData.PdfFilePath = pdfFileResult.Value;
        return Result.Ok();
    }
}
