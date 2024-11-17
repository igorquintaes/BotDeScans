using BotDeScans.App.Services;
using BotDeScans.App.Services.ExternalClients;
using BotDeScans.App.Services.Wrappers;
using FluentResults;
using Google.Apis.Download;
using Google.Apis.Drive.v3.Data;
using File = Google.Apis.Drive.v3.Data.File;
namespace BotDeScans.App.Features.GoogleDrive.InternalServices;

public class GoogleDriveFilesService(
    GoogleDriveClient googleDriveClient,
    GoogleDriveResourcesService googleDriveResourcesService,
    GoogleDrivePermissionsService googleDrivePermissionsService,
    FileService fileService,
    StreamWrapper streamWrapper,
    GoogleDriveWrapper googleDriveWrapper)
{
    public virtual Task<Result<File?>> GetFileAsync(
        string fileName,
        string? parentId,
        CancellationToken cancellationToken = default) =>
            googleDriveResourcesService.GetResourceByNameAsync(
                fileService.GetMimeType(fileName),
                fileName,
                parentId,
                cancellationToken);

    public virtual Task<Result<FileList>> GetFilesFromFolderAsync(
        string parentId,
        CancellationToken cancellationToken = default)
    {
        const int MAX_VALUE_PAGESIZE = 1000;
        var listRequest = googleDriveClient.Client.Files.List();
        listRequest.PageSize = MAX_VALUE_PAGESIZE;
        listRequest.Q =
            $"trashed = false " +
            $"and '{parentId}' in parents " +
            $"and mimeType != '{GoogleDriveFoldersService.FOLDER_MIMETYPE}'";

        return googleDriveWrapper.ExecuteAsync(listRequest, cancellationToken);
    }

    public virtual async Task<Result<File>> UploadFileAsync(
        string filePath,
        string parentId,
        bool withPublicUrl,
        CancellationToken cancellationToken = default)
    {
        var mimeType = fileService.GetMimeType(filePath);
        var fileName = Path.GetFileName(filePath);
        var file = googleDriveResourcesService.CreateResourceObject(mimeType, fileName, parentId);
        await using var stream = streamWrapper.CreateFileStream(filePath, FileMode.Open);
        var uploadRequest = googleDriveClient.Client.Files.Create(file, stream, mimeType);
        uploadRequest.Fields = "webViewLink, id";
        var uploadResult = await googleDriveWrapper.UploadAsync(uploadRequest, cancellationToken);
        if (uploadResult.IsFailed)
            return uploadResult;

        if (withPublicUrl)
        {
            var persmissionResult = await googleDrivePermissionsService.CreatePublicReaderPermissionAsync(uploadResult.Value.Id, cancellationToken);
            if (persmissionResult.IsFailed)
                return persmissionResult.ToResult();
        }

        return uploadResult;
    }

    public virtual async Task<Result<File>> UpdateFileAsync(
        string filePath,
        string oldFileId,
        CancellationToken cancellationToken = default)
    {
        var mimeType = fileService.GetMimeType(filePath);
        await using var stream = streamWrapper.CreateFileStream(filePath, FileMode.Open);
        var uploadRequest = googleDriveClient.Client.Files.Update(new(), oldFileId, stream, mimeType);
        uploadRequest.Fields = "webViewLink, id";
        return await googleDriveWrapper.UploadAsync(uploadRequest, cancellationToken);
    }

    public virtual async Task<Result> DownloadFileAsync(
        File file,
        string targetDirectory,
        CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(targetDirectory, file.Name);
        var getRequest = googleDriveClient.Client.Files.Get(file.Id);
        await using var stream = streamWrapper.CreateFileStream(filePath, FileMode.Create);
        var downloadProgress = await getRequest.DownloadAsync(stream, cancellationToken);

        return downloadProgress.Status == DownloadStatus.Completed
            ? Result.Ok()
            : Result.Fail(new Error($"Falha ao efetuar download do arquivo {file.Name} no Google Drive.")
                    .CausedBy(downloadProgress.Exception));
    }
}
