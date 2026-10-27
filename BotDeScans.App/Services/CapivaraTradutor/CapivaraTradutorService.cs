using BotDeScans.App.Infra.Logging;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Services.CapivaraTradutor.InternalServices;
using FluentResults;
using Serilog;
using System.Text.Json;

namespace BotDeScans.App.Services.CapivaraTradutor;

public class CapivaraTradutorService(
    HttpClient httpClient, 
    CapivaraTradutorUploadService capivaraTradutorUploadService)
{
    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Upload acontece na aws, não diretamente no capivara, e não podemos mandar o Bearer pra lá
    private static readonly HttpClient uploadClient = new(new HttpLoggingHandler(new HttpClientHandler()));

    public virtual async Task<Result<string>> UploadAsync(
        Info info,
        string titleId,
        string filesDirectory,
        CancellationToken cancellationToken)
    {
        var aggregateResult = Result.Ok();
        var requestForUploadResult = await capivaraTradutorUploadService.RequestForUploadAsync(
            httpClient,
            Directory.GetFiles(filesDirectory).Order(),
            cancellationToken);

        aggregateResult.WithReasons(requestForUploadResult.Reasons);
        if (aggregateResult.IsFailed)
            return aggregateResult;

        var uploadFilesResult = await capivaraTradutorUploadService.UploadFilesAsync(
            uploadClient,
            requestForUploadResult.Value,
            cancellationToken);

        aggregateResult.WithReasons(uploadFilesResult.Reasons);
        if (aggregateResult.IsFailed)
            return aggregateResult;

        var commitResult = await capivaraTradutorUploadService.CommitAsync(
            httpClient,
            requestForUploadResult.Value,
            info,
            titleId,
            cancellationToken);

        return aggregateResult.WithReasons(commitResult.Reasons);
    }

    public virtual async Task<Result> PingCredentialsAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync("https://capivaratradutor.com/api/v1/me", cancellationToken);
        var stringContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode is false)
            return HandleError(stringContent);

        var pingResponse = JsonSerializer.Deserialize<PingResponse>(stringContent, jsonSerializerOptions)!;
        if (pingResponse.Status is false)
            return HandleError(stringContent);

        return Result.Ok();

        static Result HandleError(string responseContent, string? errorMessage = null)
        {
            const string DEFAULT_COMMUNICATION_ERROR =
                "Erro ao se comunicar com o Capivara Tradutor. " +
                "Cheque o arquivo de logs para mais detalhes.";

            Log.Error(responseContent);

            return new Error(
                message: errorMessage ?? DEFAULT_COMMUNICATION_ERROR,
                causedBy: new Error(responseContent));
        }
    }

    record PingResponse(bool Status);
}