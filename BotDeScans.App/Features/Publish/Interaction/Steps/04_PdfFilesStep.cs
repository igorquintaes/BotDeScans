using BotDeScans.App.Extensions;
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

        var updatedState = state with { PdfFilePath = pdfFileResult.ValueOrDefault };

        return pdfFileResult.Map(updatedState);
    }
}
