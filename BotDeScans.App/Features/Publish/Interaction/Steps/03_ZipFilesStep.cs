using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class ZipFilesStep(
    FileService fileService,
    FileReleaseService fileReleaseService) : IManagementStep
{
    public StepType Type => StepType.Management;
    public StepName Name => StepName.ZipFiles;
    public bool IsMandatory => false;

    public Task<Result<State>> ExecuteAsync(State state, CancellationToken cancellationToken)
    {
        var zipFileResult = fileService.CreateZipFile(
            fileName: state.ChapterInfo.ChapterNumber,
            resourcesDirectory: state.OriginContentFolder,
            destinationDirectory: fileReleaseService.CreateScopedDirectory());

        if (zipFileResult.IsFailed)
            return Task.FromResult(zipFileResult.ToResult<State>());

        return Task.FromResult(Result.Ok(state with { ZipFilePath = zipFileResult.Value }));
    }
}
