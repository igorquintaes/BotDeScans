using BotDeScans.App.Services;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
namespace BotDeScans.App.Features.Publish.Steps;

public class ZipFilesStep(
    IServiceProvider serviceProvider,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.ZipFiles;
    public StepType StepType => StepType.Manage;

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

        state.InternalData.ZipFilePath = fileService.CreateZipFile(
            fileName: $"{state.Info.ChapterNumber}.zip",
            resourcesDirectory: state.InternalData.OriginContentFolder,
            destinationDirectory: fileReleaseService.CreateScopedDirectory());

        return Task.FromResult(Result.Ok());
    }
}
