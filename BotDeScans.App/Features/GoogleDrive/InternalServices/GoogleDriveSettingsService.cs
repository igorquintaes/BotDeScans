using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Services.Wrappers;
using FluentResults;
using Google.Apis.Drive.v3;
namespace BotDeScans.App.Features.GoogleDrive.InternalServices;

public class GoogleDriveSettingsService(
    DriveService driveService,
    GoogleWrapper googleWrapper,
    GoogleDriveFoldersService googleDriveFoldersService)
{
    public const string ROOT_FOLDER_NAME = "root";
    public const string BASE_FOLDER_NAME = "BOT_DIRECTORY";
    private static string _baseFolderId = null!;

    public static string BaseFolderId
    {
        get => _baseFolderId ?? throw new InvalidOperationException("Base folder not set.");
        set => _baseFolderId = value;
    }

    public virtual async Task<Result> SetUpBaseFolderAsync(CancellationToken cancellationToken)
    {
        var folderResult = await googleDriveFoldersService.GetAsync(
            BASE_FOLDER_NAME,
            ROOT_FOLDER_NAME,
            cancellationToken);

        if (folderResult.IsFailed)
            return folderResult.ToResult();

        if (folderResult.Value is not null)
        {
            BaseFolderId = folderResult.Value.Id;
            return folderResult.ToResult();
        }

        var createFolderResult = await googleDriveFoldersService.CreateAsync(
            BASE_FOLDER_NAME,
            ROOT_FOLDER_NAME,
            cancellationToken);

        if (createFolderResult.IsFailed)
            return createFolderResult.ToResult();

        BaseFolderId = createFolderResult.Value.Id;
        return Result.Ok();
    }

    public virtual async Task<Result<ConsumptionData>> GetConsumptionDataAsync(CancellationToken cancellationToken)
    {
        var aboutRequest = driveService.About.Get();
        aboutRequest.Fields = "storageQuota";

        var aboutResult = await googleWrapper.ExecuteAsync(aboutRequest, cancellationToken);
        if (aboutResult.IsFailed)
            return aboutResult.ToResult();

        var usedSpace = aboutResult.Value.StorageQuota.Usage!.Value;
        var freeSpace = aboutResult.Value.StorageQuota.Limit!.Value - usedSpace;

        return Result.Ok(new ConsumptionData(usedSpace, freeSpace));
    }
}
