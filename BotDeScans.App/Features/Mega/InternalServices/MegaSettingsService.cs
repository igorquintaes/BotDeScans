using BotDeScans.App.Models.DTOs;
using CG.Web.MegaApiClient;
using FluentResults;

namespace BotDeScans.App.Features.Mega.InternalServices;

public class MegaSettingsService(IMegaApiClient megaApiClient)
{
    private static INode? Root;

    public virtual async Task<INode> GetRootFolderAsync()
    {
        if (Root is null)
        {
            var resources = await megaApiClient.GetNodesAsync();
            Root = resources.Single(x => x.Type == NodeType.Root);
        }

        return Root;
    }

    public virtual async Task<Result<ConsumptionData>> GetConsumptionDataAsync(string nodeId)
    {
        // todo: check mega space for plans (maybe needs pr in lib)
        const long spaceTotal = 21474836480;

        var accountInfo = await megaApiClient.GetAccountInformationAsync();
        var spaceUsed = accountInfo.Metrics.Single(x => x.NodeId == nodeId).BytesUsed;
        var spaceFree = spaceTotal - spaceUsed;

        return Result.Ok(new ConsumptionData(spaceUsed, spaceFree));
    }
}
