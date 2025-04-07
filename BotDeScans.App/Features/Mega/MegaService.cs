using BotDeScans.App.Features.Mega.InternalServices;
using CG.Web.MegaApiClient;
using FluentResults;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Features.Mega;

public class MegaService(
    MegaFilesService megaFilesService,
    MegaFoldersService megaFolderService,
    IConfiguration configuration)
{
    public const string REWRITE_KEY = "Mega:RewriteExistingFile";
    public virtual async Task<Result<INode>> GetOrCreateFolderAsync(
        string folderName,
        INode parentNode)
    {
        var folderResult = await megaFolderService.GetAsync(folderName, parentNode);
        if (folderResult.IsFailed)
            return folderResult.ToResult();

        if (folderResult.ValueOrDefault is not null)
            return folderResult.ToResult(_ => folderResult.Value!);

        var newFolderNode = await megaFolderService.CreateAsync(folderName, parentNode);
        return Result.Ok(newFolderNode);
    }

    public virtual async Task<Result<Uri>> CreateFileAsync(
        string filePath,
        INode parentNode,
        CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(filePath);
        var fileResult = await megaFilesService.GetAsync(fileName, parentNode);
        if (fileResult.IsFailed)
            return fileResult.ToResult();

        if (fileResult.ValueOrDefault is not null)
        {
            var rewriteFile = configuration.GetValue<bool?>(REWRITE_KEY) ?? false;
            if (rewriteFile is false)
                return Result.Fail($"Já existe um arquivo com o nome especificado. Se desejar sobrescrever o arquivo existente, altere a configuração {REWRITE_KEY} para permitir.");

            await megaFilesService.DeleteAsync(fileResult.Value!);
        }

        var uploadUri = await megaFilesService.UploadAsync(filePath, parentNode, cancellationToken);
        return uploadUri;
    }
}
