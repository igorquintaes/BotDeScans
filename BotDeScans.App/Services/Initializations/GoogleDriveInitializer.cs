using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using FluentResults;

namespace BotDeScans.App.Services.Initializations;

public class GoogleDriveInitializer(GoogleDriveFoldersService googleDriveFoldersService)
{
    public virtual async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var aggregatedResult = new Result();
        var folderResult = await googleDriveFoldersService.GetAsync(
            GoogleDriveService.BASE_FOLDER_NAME,
            GoogleDriveService.ROOT_FOLDER_NAME,
            cancellationToken);

        aggregatedResult.WithReasons(folderResult.Reasons);
        if (aggregatedResult.IsFailed)
            return aggregatedResult;

        if (folderResult.Value is not null)
        {
            GoogleDriveService.BaseFolderId = folderResult.Value.Id;
            return aggregatedResult;
        }

        var createFolderResult = await googleDriveFoldersService.CreateAsync(
            GoogleDriveService.BASE_FOLDER_NAME,
            GoogleDriveService.ROOT_FOLDER_NAME,
            cancellationToken);

        aggregatedResult.WithReasons(createFolderResult.Reasons);
        if (createFolderResult.IsFailed)
            return aggregatedResult;

        GoogleDriveService.BaseFolderId = createFolderResult.Value.Id;

        return aggregatedResult;
    }
}
