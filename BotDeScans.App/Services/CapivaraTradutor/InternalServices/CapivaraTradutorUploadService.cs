using BotDeScans.App.Models.DTOs;
using FluentResults;
using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using File = System.IO.File;

namespace BotDeScans.App.Services.CapivaraTradutor.InternalServices;

public class CapivaraTradutorUploadService
{
    public async Task<Result<ImmutableArray<UploadData>>> RequestForUploadAsync(
        HttpClient httpClient,
        IEnumerable<string> filesPaths,
        CancellationToken cancellationToken)
    {
        const string URL = "https://capivaratradutor.com/api/v1/uploads";
        const string CONTENT_TYPE = "image/png"; // image compression always creates a PNG file

        var uploadFilesContent = filesPaths.Select(x => new
        {
            contentType = CONTENT_TYPE,
            size = new FileInfo(x).Length
        });

        var httpResponse = await httpClient.PostAsJsonAsync(URL, new { file = uploadFilesContent }, cancellationToken);
        if (httpResponse.IsSuccessStatusCode is false)
            return Result.Fail(await httpResponse.Content.ReadAsStringAsync(cancellationToken));

        var response = await httpResponse.Content.ReadFromJsonAsync<RequestForUploadResponse>(cancellationToken);
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

    public async Task<Result> UploadFilesAsync(
        HttpClient httpClient,
        IEnumerable<UploadData> uploadData,
        CancellationToken cancellationToken)
    {
        foreach (var data in uploadData)
        {
            using var fileStream = File.OpenRead(data.Path);
            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

            var response = await httpClient.PutAsync(data.UploadUrl, content, cancellationToken);

            if (response.IsSuccessStatusCode is false)
                return Result.Fail(await response.Content.ReadAsStringAsync(cancellationToken));
        }

        return Result.Ok();
    }

    public async Task<Result<string>> CommitAsync(
        HttpClient httpClient,
        IEnumerable<UploadData> uploadData,
        Info info,
        string titleId,
        CancellationToken cancellationToken)
    {;
        const string COMMIT_URL = "https://capivaratradutor.com/api/v1/works/{0}/chapters";
        const string CHAPTER_URL = "https://capivaratradutor.com/{0}/manga/{1}/chapters/{2}?page=1";

        var chapterNumberParsed = decimal.TryParse(info.ChapterNumber, out var chapterNumber);
        if (chapterNumberParsed is false)
        {
            var sanitedChapterNumber = new string([.. info.ChapterNumber.Where(c => 
                char.IsDigit(c) || 
                char.IsPunctuation(c))]);

            chapterNumber = decimal.Parse(sanitedChapterNumber);
        }

        var commitRequest = new CommitRequest(
            Number: chapterNumber,
            Title: info.ChapterName,
            Pages: [.. uploadData.Select(x => x.Key)],
            VolumeNumber: info.ChapterVolume,
            DisplayNumber: null,
            AnnouncePage: null,
            PublishAt: null,
            Notify: null);

        var commitUrl = string.Format(COMMIT_URL, titleId);

        var httpResponse = await httpClient.PostAsJsonAsync(commitUrl, commitRequest, cancellationToken);
        if (httpResponse.IsSuccessStatusCode is false)
            return Result.Fail(await httpResponse.Content.ReadAsStringAsync(cancellationToken));

        var groupId = httpClient.DefaultRequestHeaders.GetValues("GroupId").Single();
        var chapterUrl = string.Format(CHAPTER_URL, groupId, titleId, info.ChapterNumber);

        return Result.Ok(chapterUrl);
    }

    private record RequestForUploadResponse(bool Status, RequestForUploadDataResponse Data);

    private record RequestForUploadDataResponse(RequestForUploadsLinksResponse[] Uploads);

    private record RequestForUploadsLinksResponse(string Key, string UploadUrl);

    public record UploadData(string UploadUrl, string Key, string Path);

    private record CommitRequest(
        decimal Number,       // obrigatório: número do capítulo (aceita decimais, ex.: 12.5).
        string? Title,        // opcional, máx. 256 caracteres.
        string[] Pages,       // chaves devolvidas por /uploads, em ordem de leitura (1 a 500). Só são aceitas chaves de capítulos do seu próprio scan. Cada uma pode ir como { "key", "single": true } para marcá-la como página simples.
        string? VolumeNumber, // número do volume (inteiro).
        int? DisplayNumber,   // número exibido se não for o real.
        int? AnnouncePage,    // página destacada: a que aparece como imagem grande nos avisos dos seus webhooks e na prévia da obra. Número da página (1 = a primeira) ou null para usar a capa. Sem o campo, usa a página padrão do scan.
        DateTime? PublishAt,  // opcional: data ISO futura (máx. 1 ano) para agendar. Sem ela o capítulo é publicado na hora e saem os avisos de sempre (seguidores, webhooks, Discord), salvo com notify: false.
        bool? Notify);        // false = capítulo silencioso: sem avisos a seguidores, e-mails, webhooks, Discord nem La Charca (útil para enviar capítulos antigos). Padrão true. Não se combina com earlyAccess.
}

