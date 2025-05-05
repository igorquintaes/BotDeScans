using BotDeScans.App.Services.Wrappers;
using Box.V2;
using Box.V2.Models;
namespace BotDeScans.App.Services;

public class BoxService(
    StreamWrapper streamWrapper,
    IBoxClient boxClient)
{
    public const string ROOT_ID = "0";

    public virtual async Task<BoxFolder> GetOrCreateFolderAsync(string folderName, string parentFolderId = ROOT_ID)
    {
        // Todo: There is a limit of 1k folders that can be retrieved in a request.
        // Is not expected to reach this quantity in a single folder,
        // so pagination will be ignored for now to priorize other developments.
        const int maxItemsQuery = 1000;
        const string folderType = "folder";
        var folderItems = await boxClient.FoldersManager.GetFolderItemsAsync(parentFolderId, maxItemsQuery);
        var folder = folderItems.Entries.FirstOrDefault(x =>
            x.Name == folderName &&
            x.Type == folderType);

        return folder as BoxFolder
            ?? await boxClient.FoldersManager.CreateAsync(new BoxFolderRequest
            {
                Name = folderName,
                Parent = new BoxRequestEntity() { Id = parentFolderId }
            });
    }

    public virtual async Task<BoxFile> CreateFileAsync(string filePath, string parentFolderId = ROOT_ID)
    {
        var fileName = Path.GetFileName(filePath);
        var req = new BoxFileRequest()
        {
            Name = fileName,
            Parent = new BoxRequestEntity() { Id = parentFolderId }
        };

        await using var stream = streamWrapper.CreateFileStream(filePath, FileMode.Open);
        var newFile = await boxClient.FilesManager.UploadAsync(req, stream);
        var boxResult = await boxClient.FilesManager.CreateSharedLinkAsync(
            newFile.Id,
            new BoxSharedLinkRequest()
            {
                Access = BoxSharedLinkAccessType.open,
                Permissions = new BoxPermissionsRequest { Download = true },
                UnsharedAt = null
            });

        return boxResult;
    }
}
