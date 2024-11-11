using FluentResults;
using Google.Apis.Requests;
using Google.Apis.Upload;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Services.Wrappers;

[ExcludeFromCodeCoverage(Justification = @"
Needs a lot of inner wrappers to mock real GoogleDrive calls. 
This class itself works a wrapper for mostly method calls.
Maybe we should consider integration testing with real GoogleDrive API.
-> It is due Google SDK not providing an emulator to Drive, like PubSub or Bigtable.")]
public class GoogleDriveWrapper
{
    public virtual Task<Result<TResponse>> ExecuteAsync<TResponse>(
        IClientServiceRequest<TResponse> listRequest,
        CancellationToken cancellationToken)
            => Result.Try(() => listRequest.ExecuteAsync(cancellationToken),
                          ex => new Error("Não foi possível realizar a operação com o GoogleDrive.")
                                   .CausedBy(ex));

    public virtual async Task<Result<TResponse>> UploadAsync<TRequest, TResponse>(
        ResumableUpload<TRequest, TResponse> resumableUpload,
        CancellationToken cancellationToken)
    {
        var uploadProgress = await resumableUpload.UploadAsync(cancellationToken);
        return uploadProgress.Status == UploadStatus.Completed
            ? Result.Ok(resumableUpload.ResponseBody)
            : Result.Fail(new Error("Não foi possível realizar o upload.")
                    .CausedBy(uploadProgress.Exception));
    }
}
