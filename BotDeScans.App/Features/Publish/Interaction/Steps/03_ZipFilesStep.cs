using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class ZipFilesStep(
    FileService fileService,
    FileReleaseService fileReleaseService) : IConversionStep
{
    public StepType Type => StepType.Conversion;
    public StepName Name => StepName.ZipFiles;
    public bool IsMandatory => false;

    public async Task<Result<State>> ExecuteAsync(State state, CancellationToken cancellationToken)
    {
        var zipFileResult = await fileService.CreateZipFileAsync(
            fileName: state.ChapterInfo.ChapterNumber,
            resourcesDirectory: state.OriginContentFolder,
            destinationDirectory: fileReleaseService.CreateScopedDirectory(),
            cancellationToken: cancellationToken);

        if (zipFileResult.IsFailed)
            return zipFileResult.ToResult<State>();

        return Result.Ok(state with { ZipFilePath = zipFileResult.Value });
    }
}
