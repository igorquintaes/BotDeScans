using BotDeScans.App.Extensions;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using FluentResults;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using File = Google.Apis.Drive.v3.Data.File;
namespace BotDeScans.App.Features.GoogleDrive;

public class GoogleDriveService(
    GoogleDriveFilesService googleDriveFilesService,
    GoogleDriveFoldersService googleDriveFoldersService,
    GoogleDriveResourcesService googleDriveResourcesService,
    GoogleDrivePermissionsService googleDrivePermissionsService,
    IValidator<IList<File>> validator,
    IConfiguration configuration)
{
    public const string REWRITE_KEY = "GoogleDrive:RewriteExistingFile";

    public virtual async Task<Result<File>> GetOrCreateFolderAsync(
        string folderName,
        string? parentId,
        CancellationToken cancellationToken)
    {
        var folderResult = await googleDriveFoldersService.GetAsync(folderName, parentId, cancellationToken);
        if (folderResult.IsFailed)
            return folderResult.ToResult<File>();

        if (folderResult.ValueOrDefault is not null)
            return folderResult.ToResult(_ => folderResult.Value!);

        return await googleDriveFoldersService.CreateAsync(folderName, parentId, cancellationToken);
    }

    public virtual async Task<Result<File>> CreateFileAsync(
        string filePath,
        string parentId,
        bool publicAccess,
        CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(filePath);
        var fileResult = await googleDriveFilesService.GetAsync(fileName, parentId, cancellationToken);
        if (fileResult.IsFailed)
            return fileResult.ToResult();

        if (fileResult.ValueOrDefault is not null)
        {
            var rewriteFile = configuration.GetValue<bool?>(REWRITE_KEY) ?? false;
            if (rewriteFile is false)
                return Result.Fail($"Já existe um arquivo com o nome especificado. Se desejar sobrescrever o arquivo existente, altere a configuração {REWRITE_KEY} para permitir.");

            return await googleDriveFilesService.UpdateAsync(filePath, fileResult.Value!.Id, cancellationToken);
        }

        return await googleDriveFilesService.UploadAsync(filePath, parentId, publicAccess, cancellationToken);
    }

    public virtual async Task<Result> DeleteFileByNameAndParentNameAsync(
        string fileName,
        string parentFolderName,
        CancellationToken cancellationToken)
    {
        var folderResult = await googleDriveFoldersService.GetAsync(parentFolderName, GoogleDriveSettingsService.BaseFolderId, cancellationToken);
        if (folderResult.IsFailed || folderResult.Value is null)
            return folderResult.ToResult().WithConditionalError(
                conditionToAddError: () => folderResult.IsSuccess,
                error: "Não foi encontrada uma pasta com o nome especificado.");

        var fileResult = await googleDriveFilesService.GetAsync(fileName, folderResult.Value.Id, cancellationToken);
        if (fileResult.IsFailed || fileResult.Value is null)
            return fileResult.ToResult().WithConditionalError(
                conditionToAddError: () => fileResult.IsSuccess,
                error: "Não foi encontrado um arquivo com o nome especificado.");

        var deleteResult = await googleDriveResourcesService.DeleteResource(fileResult.Value.Id, cancellationToken);
        return deleteResult.ToResult();
    }

    public virtual Result<string> GetFolderIdFromUrl(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Authority != "drive.google.com")
            return Result.Fail("O link informado é inválido."); ;

        var resourceId = url
            .Replace("?id=", "/")
            .Replace("?usp=sharing", "")
            .Replace("?usp=share_link", "")
            .Split("/")
            .Last();


        return resourceId.Length != 33
            ? Result.Fail("O link informado é inválido.")
            : Result.Ok(resourceId);
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

    public virtual async Task<Result> ValidateFilesAsync(
        string folderId,
        CancellationToken cancellationToken)
    {
        var fileListResult = await googleDriveFilesService.GetManyAsync(folderId, cancellationToken);
        if (fileListResult.IsFailed)
            return fileListResult.ToResult();

        var validationResult = await validator.ValidateAsync(fileListResult.Value, cancellationToken);
        return validationResult.ToResult();
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
        if (getPermissionsResult.IsFailed)
            return getPermissionsResult.ToResult();

        return await googleDrivePermissionsService.DeleteUserReaderPermissionsAsync(getPermissionsResult.Value, GoogleDriveSettingsService.BaseFolderId, cancellationToken);
    }
}
