using BotDeScans.App.Services.ExternalClients;
using CG.Web.MegaApiClient;
using FluentResults;
namespace BotDeScans.App.Features.Mega.InternalServices;

public class MegaFoldersService(
    MegaResourcesService megaResourcesService,
    MegaClient megaClient)
{
    private readonly IMegaApiClient megaApiClient = megaClient.Client;

    public virtual async Task<Result<INode?>> GetAsync(string folderName, INode parentNode)
    {
        var resourcesResult = await megaResourcesService.GetResourcesAsync(
            name: folderName,
            parentId: parentNode.Id,
            nodeType: NodeType.Directory);

        var resources = resourcesResult.ToList();

        return resources.Count > 1
            ? Result.Fail($"Mais de um resultado foi encontrado para a busca de diretórios no Mega. " +
                          $"folderName: {folderName}, parentId: {parentNode.Name}")
            : Result.Ok(resources.SingleOrDefault());
    }

    public virtual async Task<INode> CreateAsync(string folderName, INode parentNode)
        => await megaApiClient.CreateFolderAsync(folderName, parentNode);

    public virtual async Task DeleteAsync(INode folderNode) =>
        await megaResourcesService.DeleteAsync(folderNode);
}
