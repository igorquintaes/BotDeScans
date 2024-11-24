using BotDeScans.App.Extensions;
using FluentResults;
using MangaDexSharp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Services;

public class MangaDexService(
    IMangaDex mangaDex,
    IConfiguration configuration)
{
    private string? accessToken;

    public async Task<Result> LoginAsync()
    {
        var username = configuration.GetValue<string>("Mangadex:Username");
        var password = configuration.GetValue<string>("Mangadex:Password");
        var clientId = configuration.GetValue<string>("Mangadex:ClientId");
        var clientSecret = configuration.GetValue<string>("Mangadex:ClientSecret");

        if (string.IsNullOrWhiteSpace(clientId))
            return Result.Fail("No mangadex username defined");

        if (string.IsNullOrWhiteSpace(clientSecret))
            return Result.Fail("No mangadex password defined");

        var result = await mangaDex.Auth.Personal(clientId, clientSecret, username, password);

        if (result is null || 
            result.ExpiresIn is null ||
            result.ExpiresIn <= 0)
            return Result.Fail("Unable to login in mangadex.");

        accessToken = result.AccessToken;
        return Result.Ok();
    }

    public async Task<Result> ClearPendingUploadsAsync()
    {
        var uploadResponse = await mangaDex.Upload.Get(accessToken);
        if (uploadResponse.Errors.Any(x => x.Status == StatusCodes.Status404NotFound))
            return Result.Ok();

        if (uploadResponse.ErrorOccurred)
            return uploadResponse.AsFailResult();

        var sessionId = uploadResponse.Data.Id;
        var uploadDeleteResponse = await mangaDex.Upload.Abandon(sessionId, accessToken);
        if (uploadDeleteResponse.Errors.Length != 0)
            return uploadDeleteResponse.AsFailResult();

        return Result.Ok();
    }

    public async Task<Result<string>> UploadChapterAsync(
        string mangaName,
        string? title,
        string chapterNumber,
        string? volume,
        string filesDirectory)
    {
        var groupId = configuration.GetValue<string>("Mangadex:GroupId");

        if (string.IsNullOrWhiteSpace(groupId))
            return Result.Fail("Mangadex group id is not defined.");

        if (!TryGetMangaId(mangaName, out var mangaId))
            return Result.Fail("Unable to find manga id to upload to MangaDex.");

        // todo: permitir múltiplos grupos
        var uploadResponse = await mangaDex.Upload.Begin(mangaId!, [groupId], accessToken);
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
                Title = title,
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

    // isso vai morrer, pode continuar feio
    private static bool TryGetMangaId(string mangaName, out string mangaId) =>
        File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "mangadex-ids.txt"))
            .Select(x => x.Split("$"))
            .ToDictionary(x => x[0].Trim().ToLowerInvariant(), x => x[1].Trim())
            .TryGetValue(mangaName.ToLowerInvariant(), out mangaId!);
}
