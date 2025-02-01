using BotDeScans.App.Models;
using BotDeScans.App.Services.ExternalClients;
using CG.Web.MegaApiClient;
using FluentResults;

namespace BotDeScans.App.Features.Mega.InternalServices;

public class MegaSettingsService(MegaClient megaClient)
{
    private readonly IMegaApiClient megaApiClient = megaClient.Client;

    public virtual async Task<Result<ConsumptionData>> GetConsumptionDataAsync(string nodeId)
    {
        // todo: check mega space for plans
        const long spaceTotal = 21474836480;

        var accountInfo = await megaApiClient.GetAccountInformationAsync();
        var spaceUsed = accountInfo.Metrics.Single(x => x.NodeId == nodeId).BytesUsed;
        var spaceFree = spaceTotal - spaceUsed;

        return Result.Ok(new ConsumptionData(spaceUsed, spaceFree));
    }
}
