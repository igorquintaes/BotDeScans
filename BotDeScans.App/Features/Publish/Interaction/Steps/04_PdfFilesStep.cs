using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class PdfFilesStep(
    FileService fileService,
    FileReleaseService fileReleaseService,
    IPublishContext context) : IManagementStep
{
    public StepType Type => StepType.Management;
    public StepName Name => StepName.PdfFiles;
    public bool IsMandatory => false;

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var pdfFileResult = await fileService.CreatePdfFileAsync(
            fileName: context.ChapterInfo.ChapterNumber,
            resourcesDirectory: context.OriginContentFolder,
            destinationDirectory: fileReleaseService.CreateScopedDirectory());

        if (pdfFileResult.IsFailed)
            return pdfFileResult.ToResult();

        context.SetPdfPath(pdfFileResult.Value);
        return Result.Ok();
    }
}
