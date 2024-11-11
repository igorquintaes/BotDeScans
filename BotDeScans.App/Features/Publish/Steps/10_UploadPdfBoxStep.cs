using BotDeScans.App.Services;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
namespace BotDeScans.App.Features.Publish.Steps;

public class UploadPdfBoxStep(
    IServiceProvider serviceProvider,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.UploadPdfBox;
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
            filePath: state.InternalData.PdfFilePath,
            parentFolderId: titleFolder.Id);

        state.Links.BoxPdf = file.SharedLink.DownloadUrl;
        state.Links.BoxPdfReader = file.SharedLink.DownloadUrl
            .Split("/")
            .Last()
            .Replace(".pdf", "", StringComparison.InvariantCultureIgnoreCase);

        return Result.Ok();
    }
}
