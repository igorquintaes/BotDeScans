using BotDeScans.App.Services;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
namespace BotDeScans.App.Features.Publish.Steps;

public class ZipFilesStep(
    IServiceProvider serviceProvider,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.ZipFiles;
    public StepType StepType => StepType.Management;

    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly PublishState state = state;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var fileService = serviceProvider.GetRequiredService<FileService>();
        var fileReleaseService = serviceProvider.GetRequiredService<FileReleaseService>();

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
