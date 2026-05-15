using BotDeScans.App.Extensions;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using FluentResults;
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

    public virtual async Task<Result<File>> GetOrCreateFolderAsync(
        string folderName,
        string? parentId,
        CancellationToken cancellationToken)
    {
        var folderResult = await googleDriveFoldersService.GetAsync(folderName, parentId, cancellationToken);

        return folderResult.IsSuccess && folderResult.Value is null
            ? await googleDriveFoldersService.CreateAsync(folderName, parentId, cancellationToken)
            : folderResult!;
    }

    public virtual async Task<Result<File>> CreateFileAsync(
        string filePath,
        string parentId,
        bool publicAccess,
        CancellationToken cancellationToken)
    {
        const string DUPLICATE_FILE_ERROR = $"Já existe um arquivo com o nome especificado. Se desejar sobrescrever o arquivo existente, altere a configuração {REWRITE_KEY} para permitir.";

        var fileName = Path.GetFileName(filePath);
        var fileResult = await googleDriveFilesService.GetAsync(fileName, parentId, cancellationToken);
        if (fileResult.IsFailed)
            return fileResult.ToResult();

        if (fileResult.ValueOrDefault is not null)
        {
            var rewriteFile = configuration.GetValue<bool?>(REWRITE_KEY) ?? false;

            return await Result.OkIf(rewriteFile, DUPLICATE_FILE_ERROR)
                               .BindIfSuccessAsync(UpdateFileFuncion);
        }

        return await googleDriveFilesService.UploadAsync(filePath, parentId, publicAccess, cancellationToken);

        Task<Result<File>> UpdateFileFuncion() => googleDriveFilesService.UpdateAsync(
            filePath,
            fileResult.Value!.Id,
            cancellationToken);
    }

    public virtual async Task<Result> DeleteFileByNameAndParentNameAsync(
        string fileName,
        string parentFolderName,
        CancellationToken cancellationToken)
    {
        var folderResult = await googleDriveFoldersService.GetAsync(parentFolderName, GoogleDriveSettingsService.BaseFolderId, cancellationToken);
        if (folderResult.IsFailed || folderResult.Value is null)
            return folderResult.ToResult().FailIf(
                condition: () => folderResult.IsSuccess,
                message: "Não foi encontrada uma pasta com o nome especificado.");

        var fileResult = await googleDriveFilesService.GetAsync(fileName, folderResult.Value.Id, cancellationToken);
        if (fileResult.IsFailed || fileResult.Value is null)
            return fileResult.ToResult().FailIf(
                condition: () => fileResult.IsSuccess,
                message: "Não foi encontrado um arquivo com o nome especificado.");

        var deleteResult = await googleDriveResourcesService.DeleteResource(fileResult.Value.Id, cancellationToken);
        return deleteResult.ToResult();
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

        return new Result().WithErrors(errors);
    }

    public virtual async Task<Result> GrantReaderAccessToBotFilesAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var getPermissionsResult = await googleDrivePermissionsService.GetUserPermissionsAsync(email, GoogleDriveSettingsService.BaseFolderId, cancellationToken);
        if (getPermissionsResult.IsFailed || getPermissionsResult.Value.Any())
            return getPermissionsResult.ToResult();

        var setPermissionResult = await googleDrivePermissionsService.CreateUserReaderPermissionAsync(email, GoogleDriveSettingsService.BaseFolderId, cancellationToken);
        return setPermissionResult.ToResult();
    }

    public virtual async Task<Result> RevokeReaderAccessToBotFilesAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var getPermissionsResult = await googleDrivePermissionsService.GetUserPermissionsAsync(email, GoogleDriveSettingsService.BaseFolderId, cancellationToken);
        return getPermissionsResult.IsSuccess
            ? await googleDrivePermissionsService.DeleteUserReaderPermissionsAsync(getPermissionsResult.Value, GoogleDriveSettingsService.BaseFolderId, cancellationToken)
            : getPermissionsResult.ToResult();
    }
}
