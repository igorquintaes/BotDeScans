using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Services.CapivaraTradutor.InternalServices;
using FluentResults;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BotDeScans.App.Services.CapivaraTradutor;

public class CapivaraTradutorService(HttpClient httpClient, CapivaraTradutorUploadService capivaraTradutorUploadService)
{
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
            httpClient,
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
        using var streamContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var streamReader = new StreamReader(streamContent);
        var stringContent = await streamReader.ReadToEndAsync(cancellationToken);

        if (response.IsSuccessStatusCode is false)
            return HandleError(stringContent);

        using var jsonDoc = await JsonDocument.ParseAsync(streamContent, cancellationToken: cancellationToken);

        var isStatusTrue = jsonDoc.RootElement.GetProperty("status").GetBoolean();
        if (isStatusTrue is false)
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
}