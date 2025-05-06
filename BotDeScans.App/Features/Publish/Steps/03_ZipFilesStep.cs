using BotDeScans.App.Features.Publish.State;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public class ZipFilesStep(
    FileService fileService,
    FileReleaseService fileReleaseService,
    PublishState state) : IManagementStep
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
