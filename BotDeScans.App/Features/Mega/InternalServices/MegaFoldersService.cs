using CG.Web.MegaApiClient;
using FluentResults;
namespace BotDeScans.App.Features.Mega.InternalServices;

public class MegaFoldersService(
    MegaResourcesService megaResourcesService,
    IMegaApiClient megaApiClient)
{
    public virtual async Task<Result<INode?>> GetAsync(string folderName, INode parentNode)
    {
        var resourcesResult = await megaResourcesService.GetResourcesAsync(
            name: folderName,
            parentId: parentNode.Id,
            nodeType: NodeType.Directory);

        var resources = resourcesResult.ToList();

        // todo: tem um bug que às vezes não retorna uma pasta existente E EU NÃO SEI O PORQUÊ!
        // provavelmente precisa de algum ajuste na lib externa do Mega utilizada aqui.
        // com esse bug, cria-se duas pastas e, em algum momento, dá Vasco no resources.Count > 1.
        // por enquanto, podemos ignorar isso e conviver com N pastas até corrigir na lib externa.

        //return resources.Count > 1
        //    ? Result.Fail($"Mais de um resultado foi encontrado para a busca de diretórios no Mega. " +
        //                  $"folderName: {folderName}, parent: {parentNode.Name}")
        //    : Result.Ok(resources.SingleOrDefault());

        return Result.Ok(resources.FirstOrDefault());
    }

    public virtual async Task<INode> CreateAsync(string folderName, INode parentNode)
        => await megaApiClient.CreateFolderAsync(folderName, parentNode);

    public virtual async Task DeleteAsync(INode folderNode)
        => await megaResourcesService.DeleteAsync(folderNode);
}
