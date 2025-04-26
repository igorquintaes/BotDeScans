using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.Publish.Steps.Enums;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public class UploadPdfGoogleDriveStep(
    GoogleDriveService googleDriveService,
    PublishState state) : PublishStep
{
    public override StepType Type => StepType.Upload;
    public override StepName Name => StepName.UploadPdfGoogleDrive;

    // todo seria bom que essas verificações garantissem que armazenamento > tamanho do arquivo
    public override Task<Result> ValidateAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public override async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var titleFolderResult = await googleDriveService.GetOrCreateFolderAsync(state.Title.Name, null, cancellationToken);
        if (titleFolderResult.IsFailed)
            return titleFolderResult.ToResult();

        var fileResult = await googleDriveService.CreateFileAsync(
            filePath: state.InternalData.PdfFilePath!,
            parentId: titleFolderResult.Value.Id,
            publicAccess: true,
            cancellationToken);

        if (fileResult.IsFailed)
            return fileResult.ToResult();

        state.ReleaseLinks.DrivePdf = fileResult.Value.WebViewLink;
        return Result.Ok();
    }
}
