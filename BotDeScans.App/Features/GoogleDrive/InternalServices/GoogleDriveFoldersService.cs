using BotDeScans.App.Services.Wrappers;
using FluentResults;
using Google.Apis.Drive.v3;
using File = Google.Apis.Drive.v3.Data.File;
namespace BotDeScans.App.Features.GoogleDrive.InternalServices;

public class GoogleDriveFoldersService(
    GoogleWrapper googleWrapper,
    DriveService driveService,
    GoogleDriveResourcesService googleDriveResourcesService)
{
    public const string FOLDER_MIMETYPE = "application/vnd.google-apps.folder";

    public virtual async Task<Result<File?>> GetAsync(
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

    public virtual Task<Result<File>> CreateAsync(
        string folderName,
        string? parentId,
        CancellationToken cancellationToken)
    {
        var resource = googleDriveResourcesService.CreateResourceObject(FOLDER_MIMETYPE, folderName, parentId);
        var createRequest = driveService.Files.Create(resource);
        createRequest.Fields = "webViewLink, id";
        return googleWrapper.ExecuteAsync(createRequest, cancellationToken);
    }
}
