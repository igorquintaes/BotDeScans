using BotDeScans.App.Services;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
namespace BotDeScans.App.Features.Publish.Steps;

public class UploadZipMegaStep(
    IServiceProvider serviceProvider,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.UploadZipMega;
    public StepType StepType => StepType.Execute;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var megaService = serviceProvider.GetRequiredService<MegaService>();

        var titleFolder = await megaService.GetOrCreateFolderAsync(state.Info.DisplayTitle);
        var fileResult = await megaService.CreateFileAsync(
            filePath: state.InternalData.ZipFilePath,
            parentFolder: titleFolder,
            cancellationToken);

        if (fileResult.IsFailed)
            return fileResult.ToResult();

        state.Links.MegaZip = fileResult.Value.AbsoluteUri;
        return Result.Ok();
    }
}
