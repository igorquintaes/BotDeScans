using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class ZipFilesStep(
    FileService fileService,
    FileReleaseService fileReleaseService,
    IPublishContext context) : IManagementStep
{
    public StepType Type => StepType.Management;
    public StepName Name => StepName.ZipFiles;
    public bool IsMandatory => false;

    public Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var zipFileResult = fileService.CreateZipFile(
            fileName: context.ChapterInfo.ChapterNumber,
            resourcesDirectory: context.OriginContentFolder,
            destinationDirectory: fileReleaseService.CreateScopedDirectory());

        if (zipFileResult.IsFailed)
            return Task.FromResult(zipFileResult.ToResult());

        context.SetZipPath(zipFileResult.Value);
        return Task.FromResult(Result.Ok());
    }
}
