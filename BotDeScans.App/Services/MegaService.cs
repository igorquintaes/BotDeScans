using BotDeScans.App.Services.ExternalClients;
using BotDeScans.App.Services.Wrappers;
using CG.Web.MegaApiClient;
using FluentResults;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Services;

public class MegaService(
    MegaClient megaClient,
    StreamWrapper streamWrapper,
    IConfiguration configuration)
{
    private readonly IMegaApiClient megaApiClient = megaClient.Client;

    public virtual async Task<INode> GetOrCreateFolderAsync(string folderName, INode? parentFolder = null)
    {
        var nodes = await megaApiClient.GetNodesAsync();
        var parentNode = parentFolder ?? nodes.First(x => x.Type == NodeType.Root);
        return nodes.FirstOrDefault(x =>
            x.Name != null &&
            x.Name.Equals(folderName, StringComparison.InvariantCultureIgnoreCase) &&
            x.ParentId == parentNode.Id &&
            x.Type == NodeType.Directory)
            ?? await megaApiClient.CreateFolderAsync(folderName, parentNode);
    }

    public virtual async Task<Result<Uri>> CreateFileAsync(
        string filePath,
        INode parentFolder,
        CancellationToken cancellationToken = default)
    {
        const string rewriteKey = "Mega:RewriteExistingFile";
        var rewriteExistingFile = configuration.GetValue<bool?>(rewriteKey) ?? false;
        var nodes = await megaApiClient.GetNodesAsync(parentFolder);
        var file = nodes.FirstOrDefault(x =>
            x.Name != null &&
            x.Name.Equals(Path.GetFileName(filePath), StringComparison.InvariantCultureIgnoreCase) &&
            x.ParentId == parentFolder.Id &&
            x.Type == NodeType.File);

        if (file is not null)
        {
            if (!rewriteExistingFile)
                return Result.Fail($"Já existe um arquivo com o nome especificado. Se desejar sobrescrever o arquivo existente, altere a configuração {rewriteKey} para permitir.");

            await megaApiClient.DeleteAsync(file, false);
        }

        await using var stream = streamWrapper.CreateFileStream(filePath, FileMode.Open);
        file = await megaApiClient.UploadAsync(stream, Path.GetFileName(filePath), parentFolder, cancellationToken: cancellationToken);
        return await megaApiClient.GetDownloadLinkAsync(file);
    }
}
