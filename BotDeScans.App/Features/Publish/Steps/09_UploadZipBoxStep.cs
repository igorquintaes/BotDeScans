using BotDeScans.App.Services;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
namespace BotDeScans.App.Features.Publish.Steps;

public class UploadZipBoxStep(
    IServiceProvider serviceProvider,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.UploadZipBox;
    public StepType StepType => StepType.Publish;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var boxService = serviceProvider.GetRequiredService<BoxService>();

        var titleFolder = await boxService.GetOrCreateFolderAsync(state.Info.DisplayTitle);
        var file = await boxService.CreateFileAsync(
            filePath: state.InternalData.ZipFilePath,
            parentFolderId: titleFolder.Id);

        state.Links.BoxZip = file.SharedLink.DownloadUrl;
        return Result.Ok();
    }
}
