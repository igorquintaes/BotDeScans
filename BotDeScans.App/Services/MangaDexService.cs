using BotDeScans.App.Extensions;
using FluentResults;
using MangaDexSharp;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Services;

public partial class MangaDexService(
    MangaDexAccessToken mangaDexAccessToken,
    IMangaDex mangaDex,
    IConfiguration configuration)
{
    public virtual async Task<Result> ClearPendingUploadsAsync()
    {
        var uploadResponse = await mangaDex.Upload.Get(mangaDexAccessToken.Value);
        if (uploadResponse.Errors.Any(x => x.Status == 404))
            return Result.Ok();

        if (uploadResponse.ErrorOccurred)
            return uploadResponse.AsFailResult();

        var sessionId = uploadResponse.Data.Id;
        var uploadDeleteResponse = await mangaDex.Upload.Abandon(sessionId, mangaDexAccessToken.Value);
        if (uploadDeleteResponse.Errors.Length != 0)
            return uploadDeleteResponse.AsFailResult();

        return Result.Ok();
    }

    public virtual async Task<Result<string>> UploadChapterAsync(
        string mangadexTitleId,
        string? chapterName,
        string chapterNumber,
        string? volume,
        string filesDirectory,
        CancellationToken cancellationToken)
    {
        var groupId = configuration.GetRequiredValue<string>("Mangadex:GroupId");

        // todo: permitir múltiplos grupos, ou sem vínculo a grupos
        var uploadResponse = await mangaDex.Upload.Begin(mangadexTitleId, [groupId], mangaDexAccessToken.Value);
        if (uploadResponse.ErrorOccurred)
            return uploadResponse.AsFailResult();

        var uploadId = uploadResponse.Data.Id;
        var idPages = new List<string>();
        foreach (string file in Directory
            .GetFiles(filesDirectory)
            .OrderBy(x => x))
        {
            // todo: podemos aumentar até 10 arquivos enviados por vez, baseado na doc
            // apenas precisamos nos atentar ao tamanho (bytes) limite por request.
            using var stream = File.OpenRead(file);
            var fileUpload = new StreamFileUpload(Path.GetFileName(file), stream);
            var fileUploadResult = await mangaDex.Upload.Upload(uploadId, mangaDexAccessToken.Value, cancellationToken, fileUpload);
            if (fileUploadResult.ErrorOccurred)
                return fileUploadResult.AsFailResult();

            idPages.Add(fileUploadResult.Data[0].Id);
        }

        var uploadSessionData = new UploadSessionCommit
        {
            Chapter = new()
            {
                Chapter = chapterNumber,
                Volume = volume,
                Title = chapterName,
                TranslatedLanguage = "pt-br" // todo: parametrizar após internacionalização
            },
            PageOrder = [.. idPages]
        };

        var uploadCommitResult = await mangaDex.Upload.Commit(uploadId, uploadSessionData, mangaDexAccessToken.Value);
        if (uploadCommitResult.ErrorOccurred)
            return uploadCommitResult.AsFailResult();

        return Result.Ok(uploadCommitResult.Data.Id);
    }

    public virtual Result<string> GetTitleIdFromUrl(string url)
    {
        const int GUID_CHAR_LENGHT = 36;
        const string ID_URL_PREFIX = "/title/";

        if (Guid.TryParse(url, out var guidResult))
            return guidResult.ToString();

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Authority != "mangadex.org")
            return Result.Fail("O link informado não é da MangaDex.");

        if (url.Contains(ID_URL_PREFIX) is false)
            return Result.Fail("O link informado não é de uma página de obra.");

        var titleId = url.Substring(url.IndexOf(ID_URL_PREFIX) + ID_URL_PREFIX.Length, GUID_CHAR_LENGHT);
        return Guid.TryParse(titleId, out var titleIdResult)
            ? Result.Ok(titleIdResult.ToString())
            : Result.Fail("O link informado está em formato inválido.");
    }
}
