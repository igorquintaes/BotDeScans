using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public class ZipFilesStep(
    FileService fileService,
    FileReleaseService fileReleaseService,
    PublishState state) : ManagementStep
{
    public override StepName Name => StepName.ZipFiles;

    public override Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var zipFileResult = fileService.CreateZipFile(
            fileName: state.ReleaseInfo.ChapterNumber,
            resourcesDirectory: state.InternalData.OriginContentFolder,
            destinationDirectory: fileReleaseService.CreateScopedDirectory());

        if (zipFileResult.IsFailed)
            return Task.FromResult(zipFileResult.ToResult());

        state.InternalData.ZipFilePath = zipFileResult.Value;
        return Task.FromResult(Result.Ok());
    }
}
