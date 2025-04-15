using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services.Wrappers;
using Box.V2;
using Box.V2.Config;
using Box.V2.JWTAuth;
using Box.V2.Models;
using FluentResults;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Services;

public class BoxService(
    StreamWrapper streamWrapper,
    IBoxClient boxClient)
{
    private const string rootFolderId = "0";

    public virtual async Task<BoxFolder> GetOrCreateFolderAsync(string folderName, string parentFolderId = rootFolderId)
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

    public virtual async Task<BoxFile> CreateFileAsync(string filePath, string parentFolderId = rootFolderId)
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

// todo: mover para um arquivo próprio depois de refatorarmos todo o uso do box
public class BoxClientFactory(IConfiguration configuration) : ClientFactory<IBoxClient>
{
    public const string CREDENTIALS_FILE_NAME = "box.json";

    public override bool ExpectedInPublishFeature => configuration
        .GetRequiredValues<StepName>("Settings:Publish:Steps", value => Enum.Parse(typeof(StepName), value))
        .Any(x => x == StepName.UploadPdfBox || x == StepName.UploadZipBox);

    public override async Task<Result<IBoxClient>> CreateAsync(
        CancellationToken cancellationToken = default)
    {
        await using var credentialStream = GetConfigFileAsStream(CREDENTIALS_FILE_NAME).Value;
        var config = BoxConfig.CreateFromJsonFile(credentialStream);
        var boxJwt = new BoxJWTAuth(config);
        var adminToken = await boxJwt.AdminTokenAsync();
        return boxJwt.AdminClient(adminToken);
    }

    public override Result ValidateConfiguration() =>
        ConfigFileExists(CREDENTIALS_FILE_NAME);

    public override async Task<Result> HealthCheckAsync(IBoxClient client, CancellationToken cancellationToken)
    {
        var accInfo = await client.FoldersManager.GetFolderItemsAsync("0", 1);
        return Result.OkIf(accInfo is not null, "Unknown error while trying to retrieve information from account.");
    }
}