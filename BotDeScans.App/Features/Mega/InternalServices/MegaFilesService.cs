using BotDeScans.App.Services.Wrappers;
using CG.Web.MegaApiClient;
using FluentResults;
namespace BotDeScans.App.Features.Mega.InternalServices;

public class MegaFilesService(
    MegaResourcesService megaResourcesService,
    StreamWrapper streamWrapper,
    IMegaApiClient megaApiClient)
{
    public virtual async Task<Result<INode?>> GetAsync(string fileName, INode parentNode)
    {
        var resourcesResult = await megaResourcesService.GetResourcesAsync(
            name: fileName,
            parentId: parentNode.Id,
            nodeType: NodeType.File);

        var resources = resourcesResult.ToList();

        return resources.Count > 1
            ? Result.Fail($"Mais de um resultado foi encontrado para a busca de arquivos no Mega. " +
                          $"fileName: {fileName}, parent: {parentNode.Name}")
            : Result.Ok(resources.SingleOrDefault());
    }

    public virtual async Task<Uri> UploadAsync(string filePath, INode parentNode, CancellationToken cancellationToken)
    {
        await using var stream = streamWrapper.CreateFileStream(filePath, FileMode.Open);

        var newFile = await megaApiClient.UploadAsync(
            stream,
            Path.GetFileName(filePath),
            parentNode,
            cancellationToken: cancellationToken);

        return await megaApiClient.GetDownloadLinkAsync(newFile);
    }

    public virtual async Task DeleteAsync(INode fileNode) =>
        await megaResourcesService.DeleteAsync(fileNode);
}
