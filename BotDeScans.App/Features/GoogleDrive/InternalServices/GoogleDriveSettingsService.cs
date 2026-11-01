using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Services.Wrappers;
using FluentResults;
using Google.Apis.Drive.v3;

namespace BotDeScans.App.Features.GoogleDrive.InternalServices;

public class GoogleDriveSettingsService(
    DriveService driveService,
    GoogleWrapper googleWrapper)
{

    public virtual async Task<Result<ConsumptionData>> GetConsumptionDataAsync(CancellationToken cancellationToken)
    {
        var aboutRequest = driveService.About.Get();
        aboutRequest.Fields = "storageQuota";

        var aboutResult = await googleWrapper.ExecuteAsync(aboutRequest, cancellationToken);
        if (aboutResult.IsFailed)
            return aboutResult.ToResult();

        var usedSpace = aboutResult.Value.StorageQuota.Usage!.Value;
        var freeSpace = aboutResult.Value.StorageQuota.Limit!.Value - usedSpace;
        var consumption = new ConsumptionData(usedSpace, freeSpace);

        return aboutResult.Map(_ => consumption);
    }
}
