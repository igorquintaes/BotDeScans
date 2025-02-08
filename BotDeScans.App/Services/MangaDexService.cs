using BotDeScans.App.Extensions;
using FluentResults;
using MangaDexSharp;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Services;

public partial class MangaDexService(
    IMangaDex mangaDex,
    IConfiguration configuration)
{
    private string? accessToken;

    public virtual async Task<Result> LoginAsync()
    {
        var username = configuration.GetValue("Mangadex:Username", string.Empty);
        var password = configuration.GetValue("Mangadex:Password", string.Empty);
        var clientId = configuration.GetValue("Mangadex:ClientId", string.Empty);
        var clientSecret = configuration.GetValue("Mangadex:ClientSecret", string.Empty);

        if (string.IsNullOrWhiteSpace(username))
            return Result.Fail("No mangadex username defined");

        if (string.IsNullOrWhiteSpace(password))
            return Result.Fail("No mangadex password defined");

        if (string.IsNullOrWhiteSpace(clientId))
            return Result.Fail("No mangadex clientId defined");

        if (string.IsNullOrWhiteSpace(clientSecret))
            return Result.Fail("No mangadex clientSecret defined");

        var result = await mangaDex.Auth.Personal(clientId, clientSecret, username, password);

        if (result is null ||
            result.ExpiresIn is null ||
            result.ExpiresIn <= 0 ||
            string.IsNullOrWhiteSpace(result.AccessToken))
            return Result.Fail("Unable to login in mangadex.");

        accessToken = result.AccessToken;
        return Result.Ok();
    }

    public virtual async Task<Result> ClearPendingUploadsAsync()
    {
        var uploadResponse = await mangaDex.Upload.Get(accessToken);
        if (uploadResponse.Errors.Any(x => x.Status == 404))
            return Result.Ok();

        if (uploadResponse.ErrorOccurred)
            return uploadResponse.AsFailResult();

        var sessionId = uploadResponse.Data.Id;
        var uploadDeleteResponse = await mangaDex.Upload.Abandon(sessionId, accessToken);
        if (uploadDeleteResponse.Errors.Length != 0)
            return uploadDeleteResponse.AsFailResult();

        return Result.Ok();
    }

    public virtual async Task<Result<string>> UploadChapterAsync(
        string mangadexTitleId,
        string? chapterName,
        string chapterNumber,
        string? volume,
        string filesDirectory)
    {
        var groupId = configuration.GetValue<string>("Mangadex:GroupId");

        if (string.IsNullOrWhiteSpace(groupId))
            return Result.Fail("Mangadex group id is not defined.");

        // todo: permitir múltiplos grupos
        var uploadResponse = await mangaDex.Upload.Begin(mangadexTitleId, [groupId], accessToken);
        if (uploadResponse.ErrorOccurred)
            return uploadResponse.AsFailResult();

        var uploadId = uploadResponse.Data.Id;
        var idPages = new List<string>();
        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
        foreach (string file in Directory
            .GetFiles(filesDirectory)
            .Where(fileName => allowedExtensions.Any(fileName.ToLower().EndsWith))
            .OrderBy(x => x))
        {
            // todo: podemos aumentar até 10 arquivos enviados por vez, baseado na doc
            // apenas precisamos nos atentar ao tamanho (bytes) limite por request.
            using var stream = File.OpenRead(file);
            var fileUpload = new StreamFileUpload(Path.GetFileName(file), stream);
            var fileUploadResult = await mangaDex.Upload.Upload(uploadId, accessToken, fileUpload);
            if (fileUploadResult.ErrorOccurred)
                return fileUploadResult.AsFailResult();

            idPages.Add(fileUploadResult.Data[0].Id);
        }

        // todo: colocar uma regra mais clara no validator, apenas pra pessoa não digitar nenhuma merda no lugar do volume. Assim padronizamos na app
        var volumeNumber = string.Join("", volume?.Select(x => char.IsDigit(x) ? x.ToString() : "") ?? [""]).TrimStart('0');
        if (volumeNumber == string.Empty)
            volumeNumber = null;

        var uploadSessionData = new UploadSessionCommit
        {
            Chapter = new()
            {
                Chapter = chapterNumber.TrimStart('0'),
                Title = chapterName,
                Volume = volumeNumber,
                TranslatedLanguage = "pt-br"
            },
            PageOrder = [.. idPages]
        };
        var uploadCommitResult = await mangaDex.Upload.Commit(uploadId, uploadSessionData, accessToken);
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
