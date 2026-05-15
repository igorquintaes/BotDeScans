using BotDeScans.App.Services;
using BotDeScans.App.Services.Wrappers;
using FluentResults;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using File = Google.Apis.Drive.v3.Data.File;
namespace BotDeScans.App.Features.GoogleDrive.InternalServices;

public class GoogleDriveFilesService(
    DriveService driveService,
    GoogleDriveResourcesService googleDriveResourcesService,
    GoogleDrivePermissionsService googleDrivePermissionsService,
    FileService fileService,
    StreamWrapper streamWrapper,
    GoogleWrapper googleWrapper)
{
    public virtual async Task<Result<File?>> GetAsync(
        string fileName,
        string? parentId,
        CancellationToken cancellationToken = default)
    {
        var resourcesResult = await googleDriveResourcesService.GetResourcesAsync(
            mimeType: fileService.GetMimeType(fileName),
            forbiddenMimeType: default,
            name: fileName,
            parentId: parentId,
            minResult: default,
            maxResult: 1,
            cancellationToken);

        return Result.Ok(resourcesResult.ValueOrDefault?.SingleOrDefault())
                     .WithReasons(resourcesResult.Reasons);
    }

    public virtual Task<Result<IList<File>>> GetManyAsync(
        string parentId,
        CancellationToken cancellationToken = default)
    {
        const string FOLDER_MIMETYPE = "application/vnd.google-apps.folder";

        return googleDriveResourcesService.GetResourcesAsync(
            mimeType: default,
            forbiddenMimeType: FOLDER_MIMETYPE,
            name: default,
            parentId: parentId,
            minResult: default,
            maxResult: default,
            cancellationToken);
    }

    public virtual async Task<Result<File>> UploadAsync(
        string filePath,
        string parentId,
        bool withPublicUrl,
        CancellationToken cancellationToken = default)
    {
        var mimeType = fileService.GetMimeType(filePath);
        var fileName = Path.GetFileName(filePath);
        var file = googleDriveResourcesService.CreateResourceObject(mimeType, fileName, parentId);
        await using var stream = streamWrapper.CreateFileStream(filePath, FileMode.Open);
        var uploadRequest = driveService.Files.Create(file, stream, mimeType);
        uploadRequest.Fields = "webViewLink, id";
        var uploadResult = await googleWrapper.UploadAsync(uploadRequest, cancellationToken);
        if (uploadResult.IsFailed)
            return uploadResult;

        if (withPublicUrl)
        {
            var permissionResult = await googleDrivePermissionsService.CreatePublicReaderPermissionAsync(uploadResult.Value.Id, cancellationToken);
            if (permissionResult.IsFailed)
                return permissionResult.ToResult();
        }

        return uploadResult;
    }

    public virtual async Task<Result<File>> UpdateAsync(
        string filePath,
        string oldFileId,
        CancellationToken cancellationToken = default)
    {
        var mimeType = fileService.GetMimeType(filePath);
        await using var stream = streamWrapper.CreateFileStream(filePath, FileMode.Open);
        var uploadRequest = driveService.Files.Update(new(), oldFileId, stream, mimeType);
        uploadRequest.Fields = "webViewLink, id";
        return await googleWrapper.UploadAsync(uploadRequest, cancellationToken);
    }

    public virtual async Task<Result> DownloadAsync(
        File file,
        string targetDirectory,
        CancellationToken cancellationToken = default)
    {
        const string ERROR_MESSAGE = "Ocorreu um erro ao tentar baixar o arquivo {0} do Google Drive.";

        var filePath = Path.Combine(targetDirectory, file.Name);
        var getRequest = driveService.Files.Get(file.Id);

        await using var stream = streamWrapper.CreateFileStream(filePath, FileMode.Create);
        var downloadProgress = await getRequest.DownloadAsync(stream, cancellationToken);

        return Result.OkIf(
               isSuccess: downloadProgress.Status == DownloadStatus.Completed,
               error: new Error(string.Format(ERROR_MESSAGE, file.Name))
                               .CausedBy(downloadProgress.Exception));
    }
}
