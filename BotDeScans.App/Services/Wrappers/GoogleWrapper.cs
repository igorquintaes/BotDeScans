using BotDeScans.App.Extensions;
using FluentResults;
using Google.Apis.Requests;
using Google.Apis.Upload;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Services.Wrappers;

[ExcludeFromCodeCoverage(Justification = "Safe call wrapper")]
public class GoogleWrapper
{
    public const string GENERIC_ERROR = "Não foi possível realizar a operação com o GoogleDrive.";

    public virtual Task<Result<TResponse>> ExecuteAsync<TResponse>(
        IClientServiceRequest<TResponse> request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => request.ExecuteAsync(cancellationToken), cancellationToken);

    public virtual Task<Result<TResponse>> ExecuteAsync<TResponse>(
        Func<Task<TResponse>> requestFunc,
        CancellationToken cancellationToken) =>
        new Result().SafeCallAsync(
            func: () => requestFunc(),
            error: new Error(GENERIC_ERROR));

    public virtual async Task<Result<TResponse>> UploadAsync<TRequest, TResponse>(
        ResumableUpload<TRequest, TResponse> resumableUpload,
        CancellationToken cancellationToken)
    {
        const string ERROR_MESSAGE = "Não foi possível realizar o upload.";
        const string ERROR_DETAILS_MESSAGE = "Detalhes do erro no log.";

        var result = await new Result().SafeCallAsync(
            func: () => resumableUpload.UploadAsync(cancellationToken),
            error: new Error(ERROR_MESSAGE));

        if (result.IsSuccess && result.Value.Status == UploadStatus.Completed)
            return result.Map(resumableUpload.ResponseBody);

        if (result.IsSuccess && result.Value.Exception is not null)
            return result.WithError(new Error(ERROR_MESSAGE)
                         .CausedBy(result.Value.Exception))
                         .ToResult();

        var uploadStatus = result.ValueOrDefault?.Status.ToString() ?? "unknown";
        return result.WithError(new Error(ERROR_DETAILS_MESSAGE))
                     .WithError(new Error($"UploadStatus: {uploadStatus}"))
                     .ToResult();
    }
}
