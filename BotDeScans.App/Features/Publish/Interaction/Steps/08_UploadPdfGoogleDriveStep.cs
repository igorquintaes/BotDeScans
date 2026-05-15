using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class UploadPdfGoogleDriveStep(
    GoogleDriveService googleDriveService) : IPublishStep
{
    public StepType Type => StepType.Upload;
    public StepName Name => StepName.UploadPdfGoogleDrive;
    public StepName? Dependency => StepName.PdfFiles;

    // todo seria bom que essas verificações garantissem que armazenamento > tamanho do arquivo
    public Task<Result> ValidateAsync(State state, CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result<State>> ExecuteAsync(State state, CancellationToken cancellationToken)
    {
        var titleFolderResult = await googleDriveService.GetOrCreateFolderAsync(state.Title.Name, null, cancellationToken);
        if (titleFolderResult.IsFailed)
            return titleFolderResult.ToResult<State>();

        var fileResult = await googleDriveService.CreateFileAsync(
            filePath: state.PdfFilePath!,
            parentId: titleFolderResult.Value.Id,
            publicAccess: true,
            cancellationToken);

        if (fileResult.IsFailed)
            return fileResult.ToResult<State>();

        return Result.Ok(state.WithDrivePdfLink(fileResult.Value.WebViewLink));
    }
}
