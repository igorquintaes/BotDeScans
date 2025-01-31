using BotDeScans.App.Services.ExternalClients;
using CG.Web.MegaApiClient;
namespace BotDeScans.App.Features.Mega.InternalServices;

public class MegaResourcesService(
    MegaClient megaClient)
{
    private readonly IMegaApiClient megaApiClient = megaClient.Client;
    private static INode? Root;

    public virtual async Task<INode> GetRootNodeAsync()
    {
        if (Root is null)
        {
            var resources = await megaApiClient.GetNodesAsync();
            Root = resources.Single(x => x.Type == NodeType.Root);
        }

        return Root;
    }

    public virtual async Task<IEnumerable<INode>> GetResourcesAsync(
        string? name = null,
        string? parentId = null,
        NodeType? nodeType = null)
    {
        var resources = await megaApiClient.GetNodesAsync();

        return resources.Where(x =>
            (name is null || name.Equals(x.Name, StringComparison.InvariantCultureIgnoreCase)) &&
            (parentId is null || parentId == x.ParentId) &&
            (nodeType is null || nodeType == x.Type));
    }

    public virtual async Task DeleteAsync(INode resource) =>
        await megaApiClient.DeleteAsync(resource, false);
}
