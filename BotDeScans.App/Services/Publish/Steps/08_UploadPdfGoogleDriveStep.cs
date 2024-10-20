using BotDeScans.App.Enums;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
namespace BotDeScans.App.Services.Publish.Steps;

public class UploadPdfGoogleDriveStep(
    IServiceProvider serviceProvider,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.UploadPdfGoogleDrive;
    public StepType StepType => StepType.Publish;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    // todo seria bom que essas verificações garantissem que armazenamento > tamanho do arquivo
    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var googleDriveService = serviceProvider.GetRequiredService<GoogleDriveService>();

        var titleFolderResult = await googleDriveService.GetOrCreateFolderAsync(state.Info.DisplayTitle, null, cancellationToken);
        if (titleFolderResult.IsFailed)
            return titleFolderResult.ToResult();

        var fileResult = await googleDriveService.CreateFileAsync(
            filePath: state.InternalData.PdfFilePath,
            parentId: titleFolderResult.Value.Id,
            publicAccess: true,
            cancellationToken);

        if (fileResult.IsFailed)
            return fileResult.ToResult();

        state.Links.DrivePdf = fileResult.Value.WebViewLink;
        return Result.Ok();
    }
}
