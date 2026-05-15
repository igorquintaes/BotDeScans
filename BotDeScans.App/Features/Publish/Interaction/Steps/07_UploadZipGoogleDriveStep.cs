using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class UploadZipGoogleDriveStep(
    GoogleDriveService googleDriveService) : IPublishStep
{
    public StepType Type => StepType.Upload;
    public StepName Name => StepName.UploadZipGoogleDrive;
    public StepName? Dependency => StepName.ZipFiles;

    public Task<Result> ValidateAsync(State state, CancellationToken _)
        => Task.FromResult(Result.Ok());
    // todo: pegar o tamanho do arquivo e ver se tem espaço disponível no Google Drive

    public async Task<Result<State>> ExecuteAsync(State state, CancellationToken cancellationToken)
    {
        var titleFolderResult = await googleDriveService.GetOrCreateFolderAsync(state.Title.Name, default, cancellationToken);
        if (titleFolderResult.IsFailed)
            return titleFolderResult.ToResult<State>();

        var fileResult = await googleDriveService.CreateFileAsync(
            filePath: state.ZipFilePath!,
            parentId: titleFolderResult.Value.Id,
            publicAccess: true,
            cancellationToken);

        if (fileResult.IsFailed)
            return fileResult.ToResult<State>();

        return Result.Ok(state.WithDriveZipLink(fileResult.Value.WebViewLink));
    }
}
