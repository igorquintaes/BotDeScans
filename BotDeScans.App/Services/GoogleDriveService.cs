using BotDeScans.App.Extensions;
using BotDeScans.App.Services.GoogleDrive;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Google.Apis.Drive.v3.Data;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using File = Google.Apis.Drive.v3.Data.File;
namespace BotDeScans.App.Services;

public class GoogleDriveService
{
    private readonly ExtractionService extractionService;
    private readonly GoogleDriveFilesService googleDriveFilesService;
    private readonly GoogleDriveFoldersService googleDriveFoldersService;
    private readonly GoogleDriveResourcesService googleDriveResourcesService;
    private readonly GoogleDrivePermissionsService googleDrivePermissionsService;
    private readonly IValidator<FileList> validator;
    private readonly IConfiguration configuration;

    public GoogleDriveService(
        ExtractionService extractionService,
        GoogleDriveFilesService googleDriveFilesService,
        GoogleDriveFoldersService googleDriveFoldersService,
        GoogleDriveResourcesService googleDriveResourcesService,
        GoogleDrivePermissionsService googleDrivePermissionsService,
        IValidator<FileList> validator,
        IConfiguration configuration)
    {
        this.extractionService = extractionService;
        this.googleDriveFilesService = googleDriveFilesService;
        this.googleDriveFoldersService = googleDriveFoldersService;
        this.googleDriveResourcesService = googleDriveResourcesService;
        this.googleDrivePermissionsService = googleDrivePermissionsService;
        this.validator = validator;
        this.configuration = configuration;
    }

    public async Task<Result<File>> GetOrCreateFolderAsync(
        string folderName,
        string? parentId,
        CancellationToken cancellationToken = default)
    {
        var folderResult = await googleDriveFoldersService.GetFolderAsync(folderName, parentId, cancellationToken);

        return folderResult.IsSuccess && folderResult.ValueOrDefault is null
            ? await googleDriveFoldersService.CreateFolderAsync(folderName, parentId, cancellationToken)
            : folderResult!;
    }

    public async Task<Result<File>> CreateFileAsync(
        string filePath,
        string parentId,
        bool publicAccess,
        CancellationToken cancellationToken = default)
    {
        var fileName = Path.GetFileName(filePath);
        var fileResult = await googleDriveFilesService.GetFileAsync(fileName, parentId, cancellationToken);
        if (fileResult.IsFailed)
            return fileResult.ToResult();

        if (fileResult.ValueOrDefault is not null)
        {
            const string rewriteKey = "GoogleDrive:RewriteExistingFile";
            var rewriteFile = configuration.GetValue<bool?>(rewriteKey) ?? false;
            if (!rewriteFile)
                return Result.Fail($"Já existe um arquivo com o nome especificado. Se desejar sobrescrever o arquivo existente, altere a configuração {rewriteKey} para permitir.");

            return await googleDriveFilesService.UpdateFileAsync(filePath, fileResult.Value!.Id, cancellationToken);
        }

        return await googleDriveFilesService.UploadFileAsync(filePath, parentId, publicAccess, cancellationToken);
    }

    public async Task<Result> DeleteFileByNameAndParentNameAsync(
        string fileName,
        string parentName,
        CancellationToken cancellationToken = default)
    {
        var folderResult = await googleDriveFoldersService.GetFolderAsync(parentName, GoogleDriveSettingsService.BaseFolderId, cancellationToken);
        if (folderResult.IsFailed || folderResult.Value is null)
            return folderResult.ToResult().WithConditionalError(
                conditionToAddError: () => folderResult.IsSuccess,
                error: "Não foi encontrada uma pasta com o nome especificado.");

        var fileResult = await googleDriveFilesService.GetFileAsync(fileName, folderResult.Value.Id, cancellationToken);
        if (fileResult.IsFailed || fileResult.Value is null)
            return fileResult.ToResult().WithConditionalError(
                conditionToAddError: () => fileResult.IsSuccess,
                error: "Não foi encontrado um arquivo com o nome especificado.");

        return (await googleDriveResourcesService
            .DeleteResource(fileResult.Value.Id, cancellationToken))
            .ToResult();
    }

    public async Task<Result> SaveFilesFromLinkAsync(
        string link,
        string directory,
        CancellationToken cancellationToken = default)
    {
        if (!extractionService.TryExtractGoogleDriveIdFromLink(link, out var folderId))
            return Result.Fail("O link informado é inválido.");

        var fileList = await googleDriveFilesService.GetFilesFromFolderAsync(folderId, cancellationToken);
        if (fileList.IsFailed)
            return fileList.ToResult();

        var errors = new ConcurrentBag<IError>();
        await Parallel.ForEachAsync(fileList.Value.Files, cancellationToken, async (file, ct) =>
        {
            var downloadResult = await googleDriveFilesService.DownloadFileAsync(
                file, directory, cancellationToken);

            foreach (var error in downloadResult.Errors)
                errors.Add(error);
        });

        return new Result().WithErrors(errors);
    }

    public async Task<Result> ValidateFilesFromLinkAsync(
        string link,
        CancellationToken cancellationToken = default)
    {
        if (!extractionService.TryExtractGoogleDriveIdFromLink(link, out var folderId))
            return Result.Fail("O link informado é inválido.");

        var fileListResult = await googleDriveFilesService.GetFilesFromFolderAsync(folderId, cancellationToken);
        if (fileListResult.IsFailed)
            return fileListResult.ToResult();

        var validationResult = await validator.ValidateAsync(fileListResult.Value, cancellationToken);
        return validationResult.ToResult();
    }

    public async Task<Result> GrantReaderAccessToBotFilesAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var userPermissionsResult = await googleDrivePermissionsService.GetDriverAccessPermissionsAsync(email, cancellationToken);

        return userPermissionsResult.IsSuccess && !userPermissionsResult.Value.Any()
            ? (await googleDrivePermissionsService.CreateBaseUserReaderPermissionAsync(email, cancellationToken)).ToResult()
            : userPermissionsResult.ToResult();
    }

    public async Task<Result> RevokeReaderAccessToBotFilesAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var userPermissionsResult = await googleDrivePermissionsService.GetDriverAccessPermissionsAsync(email, cancellationToken);

        return userPermissionsResult.IsSuccess
            ? await googleDrivePermissionsService.DeleteBaseUserPermissionsAsync(userPermissionsResult.Value, cancellationToken)
            : userPermissionsResult.ToResult();
    }
}
