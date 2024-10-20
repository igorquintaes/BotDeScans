using BotDeScans.App.Extensions;
using BotDeScans.App.Services.ExternalClients;
using FluentResults;

namespace BotDeScans.App.Services;

public class TsukiService(TsukiClient tsukiClient)
{
    public Task<Result> LoginAsync(CancellationToken cancellationToken) 
        => tsukiClient.InitializeAsync(cancellationToken);

    public async Task<Result<string>> UploadChapterAsync(
        string mangaName,
        string zipFile,
        CancellationToken cancellationToken)
    {
        const string UPLOAD_URL = "https://tsuki-mangas.com/api/v2/chapter/versions/upload/";
        const string TITLE_URL = "https://tsuki-mangas.com/obra/{0}";

        if (!TryGetMangaId(mangaName, out var mangaId))
            return Result.Fail("Unable to find manga id to upload to Tsuki.");

        using var stream = File.OpenRead(zipFile);
        using var fileContent = new StreamContent(stream);
        using var content = new MultipartFormDataContent
        {
            { new StringContent(mangaId), "manga_id" },
            { fileContent, "files[]", Path.GetFileName(zipFile) }
        };

        content.NormalizeBoundary();
        fileContent.Headers.Add("Content-Type", "application/octet-stream");
        var uploadPageResponse = await tsukiClient.Client.PostAsync(UPLOAD_URL, content, cancellationToken);
        var uploadPageResponseContentResult = await uploadPageResponse.TryGetContent<string>(TsukiClient.Configuration, cancellationToken);
        if (uploadPageResponseContentResult.IsFailed)
            return uploadPageResponseContentResult.ToResult();

        var releaseUrl = string.Format(TITLE_URL, mangaId);
        return Result.Ok(releaseUrl);
    }

    // isso vai morrer, pode continuar feio
    private static bool TryGetMangaId(string mangaName, out string mangaId) =>
        File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "tsuki-ids.txt"))
            .Select(x => x.Split("$"))
            .ToDictionary(x => x[0].Trim().ToLowerInvariant(), x => x[1].Trim())
            .TryGetValue(mangaName.ToLowerInvariant(), out mangaId!);
}
