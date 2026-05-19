using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Wrappers;
using Box.Sdk.Gen;
using Box.Sdk.Gen.Managers;
using Box.Sdk.Gen.Schemas;
using FluentResults;
using File = Box.Sdk.Gen.Schemas.File;

namespace BotDeScans.App.Services;

public class BoxService(
    StreamWrapper streamWrapper,
    IBoxClient boxClient)
{
    public const string ROOT_ID = "0";
    public const string GENERIC_ERROR = "Um erro ocorreu durante a comunicação com o Box. Mais detalhes no log.";

    /// <summary>
    /// Todo: There is a limit of 1k folders that can be retrieved in a request.
    /// Is not expected to reach this quantity in a single folder,
    /// so pagination will be ignored for now to priorize other developments.
    /// </summary>
    /// <param name="folderName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual async Task<Result<FolderMini>> GetOrCreateFolderAsync(string folderName, CancellationToken cancellationToken = default)
    {
        var getFolderItems = await new Result().SafeCallAsync(
            async () => await boxClient.Folders.GetFolderItemsAsync(ROOT_ID, cancellationToken: cancellationToken),
            new Error(GENERIC_ERROR));

        if (getFolderItems.IsFailed)
            return getFolderItems.ToResult();

        var folder = getFolderItems.Value.Entries?.FirstOrDefault(x => x.FolderMini!.Name == folderName);

        if (folder is not null && folder.FolderMini is not null)
            return folder.FolderMini;
        
        return await new Result().SafeCallAsync<FolderMini>(
               async () => await boxClient.Folders.CreateFolderAsync(new(folderName, new(ROOT_ID)), cancellationToken: cancellationToken),
               new Error(GENERIC_ERROR));
    }

    public virtual async Task<Result<File>> CreateFileAsync(string filePath, string parentFolderId, CancellationToken cancellationToken = default)
    {
        var fileName = Path.GetFileName(filePath);

        await using var stream = streamWrapper.CreateFileStream(filePath, FileMode.Open);
        var parentField = new UploadFileRequestBodyAttributesParentField(parentFolderId);
        var attributes = new UploadFileRequestBodyAttributesField(fileName, parentField);
        var request = new UploadFileRequestBody(attributes, stream);

        var uploadFileResult = await new Result().SafeCallAsync(
            async () => await boxClient.Uploads.UploadFileAsync(request, cancellationToken: cancellationToken),
            new Error(GENERIC_ERROR));

        if (uploadFileResult.IsFailed)
            return uploadFileResult.ToResult();

        var fileId = uploadFileResult.Value.Entries!.Single().Id;
        var accessType = UpdateFileByIdRequestBodySharedLinkAccessField.Open;
        var access = new StringEnum<UpdateFileByIdRequestBodySharedLinkAccessField>(accessType);
        var updateFile = new UpdateFileByIdRequestBody()
        {
            SharedLink = new()
            {
                Access = access,
                Permissions = new() { CanDownload = true },
                UnsharedAt = null
            }
        };

        var updateFileResult = await new Result().SafeCallAsync<File>(
            async () => await boxClient.Files.UpdateFileByIdAsync(fileId, updateFile, cancellationToken: cancellationToken),
            new Error(GENERIC_ERROR));

        return updateFileResult.WithReasons(uploadFileResult.Reasons);
    }
}
