using BotDeScans.App.Models.DTOs;
using FluentResults;
using System.Collections.Immutable;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using static BotDeScans.App.Services.CapivaraTradutor.CapivaraTradutorService;

namespace BotDeScans.App.Services.CapivaraTradutor.InternalServices;

public class CapivaraTradutorUploadService
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public virtual async Task<Result<ImmutableArray<UploadData>>> RequestForUploadAsync(
        HttpClient httpClient,
        IEnumerable<string> filesPaths,
        CancellationToken cancellationToken)
    {
        const string URL = "https://capivaratradutor.com/api/v1/uploads";

        // Image compression always creates a PNG file.
        // Accepted Types: image/jpeg, image/png, image/webp, image/gif, image/avif
        const string CONTENT_TYPE = "image/png";

        var uploadFilesContent = filesPaths.Select(x => new
        {
            contentType = CONTENT_TYPE,
            size = new FileInfo(x).Length
        });

        var httpResponse = await httpClient.PostAsJsonAsync(URL, new { files = uploadFilesContent }, jsonSerializerOptions, cancellationToken);
        if (httpResponse.IsSuccessStatusCode is false)
            return Result.Fail(await httpResponse.Content.ReadAsStringAsync(cancellationToken));

        var response = await httpResponse.Content.ReadFromJsonAsync<ResponseDataBase<RequestForUploadDataResponse>>(jsonSerializerOptions, cancellationToken);
        if (response!.Status is false)
        {
            var contentBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            var error = new Error("Failed to upload files").CausedBy(contentBody);
            return error;
        }

        return filesPaths
            .Zip(response.Data.Uploads, (path, upload) => new UploadData(
                upload.UploadUrl,
                upload.Key,
                path))
            .ToImmutableArray();
    }

    public virtual async Task<Result> UploadFilesAsync(
        HttpClient httpClient,
        IEnumerable<UploadData> uploadData,
        CancellationToken cancellationToken)
    {
        foreach (var data in uploadData)
        {
            using var fileStream = new FileStream(
            data.Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

            using var request = new HttpRequestMessage(HttpMethod.Put, data.UploadUrl);
            request.Content = new StreamContent(fileStream);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode is false)
                return Result.Fail(await response.Content.ReadAsStringAsync(cancellationToken));
        }

        return Result.Ok();
    }

    public virtual async Task<Result<string>> CommitAsync(
        HttpClient httpClient,
        IEnumerable<UploadData> uploadData,
        Info info,
        string referenceId,
        CancellationToken cancellationToken)
    {
        const string COMMIT_URL = "https://capivaratradutor.com/api/v1/works/{0}/chapters";

        var culture = CultureInfo.InvariantCulture;

        var chapterNumberParsed = decimal.TryParse(info.ChapterNumber, culture, out var chapterNumber);
        if (chapterNumberParsed is false)
        {
            var sanitedChapterNumber = new string([.. info.ChapterNumber.Where(c =>
                char.IsDigit(c) ||
                char.IsPunctuation(c))]);

            chapterNumber = decimal.Parse(sanitedChapterNumber, culture);
        }

        var volume = info.ChapterVolume is null
            ? default(int?)
            : int.Parse(info.ChapterVolume);

        var commitRequest = new CommitRequest(
            Number: chapterNumber,
            Title: info.ChapterName,
            Pages: [.. uploadData.Select(x => x.Key)],
            VolumeNumber: volume);

        var commitUrl = string.Format(COMMIT_URL, referenceId);

        var httpResponse = await httpClient.PostAsJsonAsync(commitUrl, commitRequest, jsonSerializerOptions, cancellationToken);
        if (httpResponse.IsSuccessStatusCode is false)
            return Result.Fail(await httpResponse.Content.ReadAsStringAsync(cancellationToken));

        var commitResponse = await httpResponse.Content.ReadFromJsonAsync<ResponseDataBase<CommitResponse>>(jsonSerializerOptions, cancellationToken);
        if (commitResponse!.Status is false)
            return Result.Fail(await httpResponse.Content.ReadAsStringAsync(cancellationToken));

        return Result.Ok(commitResponse.Data.Url);
    }

    public record UploadData(string UploadUrl, string Key, string Path);

    private record RequestForUploadDataResponse(RequestForUploadsLinksResponse[] Uploads);

    private record RequestForUploadsLinksResponse(string Key, string UploadUrl);

    private record CommitRequest(decimal Number, string? Title, string[] Pages, int? VolumeNumber);

    private record CommitResponse(string Url);
}

