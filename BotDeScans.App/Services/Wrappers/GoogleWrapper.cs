using BotDeScans.App.Extensions;
using FluentResults;
using Google.Apis.Requests;
using Google.Apis.Upload;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Services.Wrappers;

[ExcludeFromCodeCoverage(Justification = @"
Needs a lot of inner wrappers to mock real Google calls. 
This class itself works a wrapper for mostly method calls.
Maybe we should consider integration testing with real Google API.
-> It is due Google SDK not providing an emulator to Drive, Blogger, BQ etc.")]
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
            new Error(GENERIC_ERROR));

    public virtual async Task<Result<TResponse>> UploadAsync<TRequest, TResponse>(
        ResumableUpload<TRequest, TResponse> resumableUpload,
        CancellationToken cancellationToken)
    {
        const string ERROR_MESSAGE = "Não foi possível realizar o upload.";
        const string ERROR_DETAILS_MESSAGE = "Detalhes do erro no log.";

        var result = await new Result().SafeCallAsync(
            func: () => resumableUpload.UploadAsync(cancellationToken),
            new Error(ERROR_MESSAGE));

        return result.IsSuccess && result.Value.Status == UploadStatus.Completed
             ? Result.Ok(resumableUpload.ResponseBody)
             : Result.Fail(new Error(ERROR_DETAILS_MESSAGE)
                     .CausedBy(result.ValueOrDefault?.Exception));
    }
}
