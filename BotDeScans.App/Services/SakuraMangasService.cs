using FluentResults;
using Serilog;
using System.Net.Http.Headers;
using System.Text.Json;
namespace BotDeScans.App.Services;

public class SakuraMangasService(
    HttpClient httpClient,
    string User,
    string Pass)
{
    private const string UPLOAD_URL = "https://sakuramangas.org/dist/sistema/models/mangas/api.php";

    public virtual async Task<Result<string>> UploadAsync(
        string chapterNumber,
        string? chapterName,
        string mangaDexId,
        string zipPath,
        CancellationToken cancellationToken)
    {
        // Todo: implementar funcionalidade de joint futuramente na app
        // campo 'scanparceira', string não obrigatória e igual à cadastrada na Sakura
        using var form = new MultipartFormDataContent
        {
            { new StringContent(User), "email" },
            { new StringContent(Pass), "senha" },
            { new StringContent(mangaDexId), "mangadexid" },
            { new StringContent(chapterNumber), "numchapter" }
        };

        if (string.IsNullOrWhiteSpace(chapterName) is false)
            form.Add(new StringContent(chapterName), "titulochapter");


        using var fileStream = File.OpenRead(zipPath);
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        form.Add(fileContent, "file-upload", Path.GetFileName(zipPath));

        var response = await httpClient.PostAsync(UPLOAD_URL, form, cancellationToken);
        var stringResponseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode is false)
            return HandleError(stringResponseContent);

        var objectResponse = JsonSerializer.Deserialize<SakuraRequestResponse>(stringResponseContent);
        return string.IsNullOrWhiteSpace(objectResponse!.error_code)
            ? Result.Ok(objectResponse.chapter_url!)
            : HandleError(stringResponseContent);
    }

    public async Task<Result> ValidateCredentialsAsync(CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent(User), "email" },
            { new StringContent(Pass), "senha" },
            { new StringContent(Guid.Empty.ToString()), "mangadexid" },
            { new StringContent("1"), "numchapter" }
        };

        var response = await httpClient.PostAsync(UPLOAD_URL, form, cancellationToken);
        var stringResponseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode is false)
            return HandleError(stringResponseContent);

        var objectResponse = JsonSerializer.Deserialize<SakuraRequestResponse>(stringResponseContent);
        if (objectResponse!.error_code == "MANGA_NOT_FOUND")
            return Result.Ok();

        var errorMessage = objectResponse!.error_code == "AUTH_INVALID" ? "Usuário ou senhas incorretos." : null;
        return HandleError(stringResponseContent, errorMessage);
    }

    private static Result HandleError(string responseContent, string? errorMessage = null)
    {
        Log.Error(responseContent);

        return new Error(
            message: errorMessage ?? "Erro ao se comunicar com a Sakura Mangás. Cheque o arquivo de logs para mais detalhes.", 
            causedBy: new Error(responseContent));
    }
}

// todo: usar um deserializer baseado em snake_case 
#pragma warning disable IDE1006 // Estilos de Nomenclatxura
public record SakuraRequestResponse(string? message, string? error_code, int? id_capitulo, string? chapter_url);
#pragma warning restore IDE1006 // Estilos de Nomenclatura
