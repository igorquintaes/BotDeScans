using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services.ExternalClients;
using FluentResults;
using File = Google.Apis.Drive.v3.Data.File;
namespace BotDeScans.App.Features.GoogleDrive;

public class GoogleDriveFoldersService(
    GoogleDriveClient googleDriveClient,
    GoogleDriveWrapper googleDriveWrapper,
    GoogleDriveResourcesService googleDriveResourcesService)
{
    public const string FOLDER_MIMETYPE = "application/vnd.google-apps.folder";

    public virtual async Task<Result<File?>> GetFolderAsync(
        string folderName,
        string? parentId,
        CancellationToken cancellationToken)
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

    public virtual Task<Result<File>> CreateFolderAsync(
        string folderName,
        string? parentId,
        CancellationToken cancellationToken)
    {
        var resource = googleDriveResourcesService.CreateResourceObject(FOLDER_MIMETYPE, folderName, parentId);
        var createRequest = googleDriveClient.Client.Files.Create(resource);
        createRequest.Fields = "webViewLink, id";
        return googleDriveWrapper.ExecuteAsync(createRequest, cancellationToken);
    }
}
