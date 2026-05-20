using BotDeScans.App.Features.GoogleDrive.InternalServices;
using FluentResults;
using iText.Layout.Element;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using File = Google.Apis.Drive.v3.Data.File;

namespace BotDeScans.App.Features.GoogleDrive;

public class GoogleDriveService(
    GoogleDriveFilesService googleDriveFilesService,
    GoogleDriveFoldersService googleDriveFoldersService,
    GoogleDriveResourcesService googleDriveResourcesService,
    GoogleDrivePermissionsService googleDrivePermissionsService,
    IConfiguration configuration)
{
    public const string REWRITE_KEY = "GoogleDrive:RewriteExistingFile";
    public const string FOLDER_NOT_FOUND = "Não foi encontrada uma pasta com o nome especificado.";
    public const string FILE_NOT_FOUND = "Não foi encontrado um arquivo com o nome especificado.";
    public const string DUPLICATE_FILE_ERROR =
        $"Já existe um arquivo com o nome especificado. " +
        $"Se desejar sobrescrever o arquivo existente, altere a configuração {REWRITE_KEY} para permitir.";

    public virtual async Task<Result<File>> GetOrCreateFolderAsync(
        string folderName,
        string? parentId,
        CancellationToken cancellationToken)
    {
        var getFolderResult = await googleDriveFoldersService.GetAsync(folderName, parentId, cancellationToken);
        if (getFolderResult.IsFailed || getFolderResult.Value is not null)
            return getFolderResult!;

        var createFolderResult = await googleDriveFoldersService.CreateAsync(folderName, parentId, cancellationToken);
        return createFolderResult.WithReasons(getFolderResult.Reasons);
    }

    public virtual async Task<Result<File>> UpdateOrCreateFileAsync(
        string filePath,
        string parentId,
        bool publicAccess,
        CancellationToken cancellationToken)
    {

        var fileName = Path.GetFileName(filePath);
        var fileResult = await googleDriveFilesService.GetAsync(fileName, parentId, cancellationToken);
        if (fileResult.IsFailed)
            return fileResult.ToResult();

        var rewriteFile = configuration.GetValue<bool?>(REWRITE_KEY) ?? false;

        if (fileResult.Value is { })
            return rewriteFile
                 ? await UpdateFileFuncion(fileResult.Reasons)
                 : fileResult.ToResult()
                             .WithError(DUPLICATE_FILE_ERROR)
                             .WithReasons(fileResult.Reasons);

        var uploadResult = await googleDriveFilesService.UploadAsync(
            filePath, 
            parentId, 
            cancellationToken);

        return uploadResult.WithReasons(fileResult.Reasons);

        async Task<Result<File>> UpdateFileFuncion(List<IReason> reasons)
        {
            var updateResult = await googleDriveFilesService
                .UpdateAsync(filePath, fileResult.Value!.Id, cancellationToken);

            return updateResult.WithReasons(reasons);
        }
    }

    public virtual async Task<Result> DeleteFileByNameAndParentNameAsync(
        string fileName,
        string parentFolderName,
        CancellationToken cancellationToken)
    {
        var resourceId = GoogleDriveSettingsService.BaseFolderId;

        var folderResult = await googleDriveFoldersService.GetAsync(parentFolderName, resourceId, cancellationToken);
        if (folderResult.IsFailed)
            return folderResult.ToResult();

        if (folderResult.Value is null)
            return folderResult.WithError(FOLDER_NOT_FOUND)
                               .ToResult();

        var fileResult = await googleDriveFilesService.GetAsync(fileName, folderResult.Value.Id, cancellationToken);
        if (fileResult.IsFailed)
            return fileResult.ToResult();

        if (fileResult.Value is null)
            return fileResult.WithError(FILE_NOT_FOUND)
                             .WithReasons(folderResult.Reasons)
                             .ToResult();

        var deleteResult = await googleDriveResourcesService.DeleteResource(fileResult.Value.Id, cancellationToken);
        return deleteResult
              .WithReasons(folderResult.Reasons)
              .WithReasons(fileResult.Reasons)
              .ToResult();
    }

    public virtual async Task<Result> SaveFilesAsync(
        string folderId,
        string directory,
        CancellationToken cancellationToken)
    {
        var fileList = await googleDriveFilesService.GetManyAsync(folderId, cancellationToken);
        if (fileList.IsFailed)
            return fileList.ToResult();

        var errors = new ConcurrentBag<IError>();
        await Parallel.ForEachAsync(fileList.Value, cancellationToken, async (file, ct) =>
        {
            var downloadResult = await googleDriveFilesService.DownloadAsync(
                file, directory, cancellationToken);

            foreach (var error in downloadResult.Errors)
                errors.Add(error);
        });

        return new Result()
            .WithErrors(errors)
            .WithReasons(fileList.Reasons);
    }

    public virtual async Task<Result> GrantReaderAccessToBotFilesAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var resourceId = GoogleDriveSettingsService.BaseFolderId;

        var getPermissionsResult = await googleDrivePermissionsService
            .GetUserPermissionsAsync(email, resourceId, cancellationToken);

        if (getPermissionsResult.IsFailed || 
            getPermissionsResult.Value.Any())
            return getPermissionsResult.ToResult();

        var setPermissionResult = await googleDrivePermissionsService
            .CreateUserReaderPermissionAsync(email, resourceId, cancellationToken);

        return setPermissionResult
            .WithReasons(getPermissionsResult.Reasons)
            .ToResult();
    }

    public virtual async Task<Result> RevokeReaderAccessToBotFilesAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var resourceId = GoogleDriveSettingsService.BaseFolderId;
        var getPermissionsResult = await googleDrivePermissionsService
            .GetUserPermissionsAsync(email, resourceId, cancellationToken);

        if (getPermissionsResult.IsFailed)
            return getPermissionsResult.ToResult();

        var deletePermissionResult = await googleDrivePermissionsService
            .DeleteUserReaderPermissionsAsync(getPermissionsResult.Value, resourceId, cancellationToken);

        return deletePermissionResult
            .WithReasons(getPermissionsResult.Reasons);
    }
}
