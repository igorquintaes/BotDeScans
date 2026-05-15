using BotDeScans.App.Services.Wrappers;
using Box.Sdk.Gen;
using Box.Sdk.Gen.Managers;
using Box.Sdk.Gen.Schemas;
using File = Box.Sdk.Gen.Schemas.File;

namespace BotDeScans.App.Services;

public class BoxService(
    StreamWrapper streamWrapper,
    IBoxClient boxClient)
{
    public const string ROOT_ID = "0";

    /// <summary>
    /// Todo: There is a limit of 1k folders that can be retrieved in a request.
    /// Is not expected to reach this quantity in a single folder,
    /// so pagination will be ignored for now to priorize other developments.
    /// </summary>
    /// <param name="folderName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual async Task<FolderMini> GetOrCreateFolderAsync(string folderName, CancellationToken cancellationToken = default)
    {
        var folderItems = await boxClient.Folders.GetFolderItemsAsync(ROOT_ID, cancellationToken: cancellationToken);
        var folder = folderItems.Entries?.FirstOrDefault(x => x.FolderMini!.Name == folderName);

        return folder?.FolderMini ?? 
            await boxClient.Folders.CreateFolderAsync(
                requestBody: new(folderName, new(ROOT_ID)),
                cancellationToken: cancellationToken);
    }

    public virtual async Task<File> CreateFileAsync(string filePath, string parentFolderId, CancellationToken cancellationToken = default)
    {
        var fileName = Path.GetFileName(filePath);
        await using var stream = streamWrapper.CreateFileStream(filePath, FileMode.Open);

        var parentField = new UploadFileRequestBodyAttributesParentField(parentFolderId);
        var attributes = new UploadFileRequestBodyAttributesField(fileName, parentField);
        var request = new UploadFileRequestBody(attributes, stream);
        var newFile = await boxClient.Uploads.UploadFileAsync(request, cancellationToken: cancellationToken);
        var accessType = UpdateFileByIdRequestBodySharedLinkAccessField.Open;
        var updateFile = new UpdateFileByIdRequestBody()
        {
            SharedLink = new()
            {
                Access = new StringEnum<UpdateFileByIdRequestBodySharedLinkAccessField>(accessType),
                Permissions = new() { CanDownload = true },
                UnsharedAt = null
            }
        };

        return await boxClient.Files.UpdateFileByIdAsync(
            newFile.Entries!.Single().Id,
            updateFile,
            cancellationToken: cancellationToken);
    }
}
