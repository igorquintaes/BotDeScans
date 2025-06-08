using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class ZipFilesStep(
    FileService fileService,
    FileReleaseService fileReleaseService,
    State state) : IManagementStep
{
    public StepType Type => StepType.Management;
    public StepName Name => StepName.ZipFiles;
    public bool IsMandatory => false;

    public Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var zipFileResult = fileService.CreateZipFile(
            fileName: state.ChapterInfo.ChapterNumber,
            resourcesDirectory: state.InternalData.OriginContentFolder,
            destinationDirectory: fileReleaseService.CreateScopedDirectory());

        if (zipFileResult.IsFailed)
            return Task.FromResult(zipFileResult.ToResult());

        state.InternalData.ZipFilePath = zipFileResult.Value;
        return Task.FromResult(Result.Ok());
    }
}
