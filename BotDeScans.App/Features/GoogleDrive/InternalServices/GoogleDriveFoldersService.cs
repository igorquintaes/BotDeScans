using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services.ExternalClients;
using FluentResults;
using File = Google.Apis.Drive.v3.Data.File;
namespace BotDeScans.App.Features.GoogleDrive;

public class GoogleDriveFoldersService(
    GoogleDriveClient googleDriveClient,
    GoogleDriveResourcesService googleDriveResourcesService,
    GoogleDriveWrapper googleDriveWrapper)
{
    public const string FOLDER_MIMETYPE = "application/vnd.google-apps.folder";

    public virtual async Task<Result<File?>> GetFolderAsync(
        string folderName,
        string? parentId,
        CancellationToken cancellationToken = default)
    {
        var resourcesResult = await googleDriveResourcesService.GetResourcesAsync(
            mimeType: FOLDER_MIMETYPE,
            forbiddenMimeType: default,
            name: folderName,
            parentId: parentId,
            minResult: default,
            maxResult: 1,
            cancellationToken);

        if (resourcesResult.IsFailed)
            return resourcesResult.ToResult();

        return resourcesResult.Value.SingleOrDefault();
    }

    public virtual async Task<Result<File?>> GetFolderByIdAsync(
        string folderId,
        CancellationToken cancellationToken = default)
    {
        var getRequest = googleDriveClient.Client.Files.Get(folderId);
        var folderResult = await googleDriveWrapper.ExecuteAsync(getRequest, cancellationToken);

        return folderResult.IsFailed
            || folderResult.ValueOrDefault is null
            || folderResult.Value.MimeType == FOLDER_MIMETYPE
                ? folderResult
                : Result.Ok<File?>(null);
    }

    public virtual Task<Result<File>> CreateFolderAsync(
        string folderName,
        string? parentId,
        CancellationToken cancellationToken = default)
    {
        var resource = googleDriveResourcesService.CreateResourceObject(FOLDER_MIMETYPE, folderName, parentId);
        var createRequest = googleDriveClient.Client.Files.Create(resource);
        createRequest.Fields = "webViewLink, id";
        return googleDriveWrapper.ExecuteAsync(createRequest, cancellationToken);
    }
}
