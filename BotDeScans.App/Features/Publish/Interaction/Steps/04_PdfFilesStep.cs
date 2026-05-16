using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class PdfFilesStep(
    FileService fileService,
    FileReleaseService fileReleaseService) : IConversionStep
{
    public StepType Type => StepType.Conversion;
    public StepName Name => StepName.PdfFiles;
    public bool IsMandatory => false;

    public async Task<Result<State>> ExecuteAsync(State state, CancellationToken cancellationToken)
    {
        var pdfFileResult = await fileService.CreatePdfFileAsync(
            fileName: state.ChapterInfo.ChapterNumber,
            resourcesDirectory: state.OriginContentFolder,
            destinationDirectory: fileReleaseService.CreateScopedDirectory());

        if (pdfFileResult.IsFailed)
            return pdfFileResult.ToResult<State>();

        return Result.Ok(state with { PdfFilePath = pdfFileResult.Value });
    }
}
