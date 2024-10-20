using BotDeScans.App.Services.ExternalClients;
using BotDeScans.App.Services.Wrappers;
using FluentResults;
namespace BotDeScans.App.Services.GoogleDrive;

public class GoogleDriveSettingsService(
    GoogleDriveClient googleDriveClient,
    GoogleDriveWrapper googleDriveWrapper,
    GoogleDriveFoldersService googleDriveFoldersService)
{
    public const string BaseFolderName = "BOT_DIRECTORY";
    private static string _baseFolderId = null!;

    public static string BaseFolderId
    {
        get => _baseFolderId ?? throw new InvalidOperationException("Base folder not set.");
        set => _baseFolderId = value;
    }

    public async Task<Result> SetUpBaseFolderAsync(CancellationToken cancellationToken = default)
    {
        const string GOOGLE_DRIVE_BASEST_FOLDER = "root";
        var folderResult = await googleDriveFoldersService.GetFolderAsync(
            BaseFolderName,
            GOOGLE_DRIVE_BASEST_FOLDER,
            cancellationToken);

        if (folderResult.IsFailed)
            return folderResult.ToResult();

        var folder = folderResult.ValueOrDefault;
        if (folder is null)
        {
            var createFolderResult = await googleDriveFoldersService.CreateFolderAsync(
                BaseFolderName,
                GOOGLE_DRIVE_BASEST_FOLDER,
                cancellationToken);

            if (createFolderResult.IsFailed)
                return createFolderResult.ToResult();

            folder = createFolderResult.Value;
        }

        BaseFolderId = folder.Id;
        return Result.Ok();
    }

    public virtual async Task<Result<ConsumptionData>> GetConsumptionData(CancellationToken cancellationToken = default)
    {
        var aboutRequest = googleDriveClient.Client.About.Get();
        aboutRequest.Fields = "storageQuota";

        var aboutResult = await googleDriveWrapper.ExecuteAsync(aboutRequest, cancellationToken);
        if (aboutResult.IsFailed)
            return aboutResult.ToResult();

        var usedSpace = aboutResult.Value.StorageQuota.Usage!.Value;
        var freeSpace = aboutResult.Value.StorageQuota.Limit!.Value - usedSpace;

        return Result.Ok(new ConsumptionData(usedSpace, freeSpace));
    }

    public record ConsumptionData(long UsedSpace, long FreeSpace);
}
