using BotDeScans.App.Services.Wrappers;
using FluentResults;
using Google.Apis.Drive.v3;
using File = Google.Apis.Drive.v3.Data.File;

namespace BotDeScans.App.Features.GoogleDrive.InternalServices;

public class GoogleDriveResourcesService(
    DriveService driveService,
    GoogleWrapper googleWrapper)
{
    public virtual async Task<Result<IList<File>>> GetResourcesAsync(
        string? mimeType,
        string? forbiddenMimeType,
        string? name,
        string? parentId,
        int? minResult,
        int? maxResult,
        CancellationToken cancellationToken = default)
    {
        var query = new GoogleDriveQueryBuilder()
            .WithMimeType(mimeType)
            .WithoutMimeType(forbiddenMimeType)
            .WithName(name)
            .WithParent(parentId)
            .Build();

        var listRequest = driveService.Files.List();
        listRequest.Q = query;
        listRequest.PageSize = 1000;
        listRequest.Fields = "files(*)";
        var requestResult = await googleWrapper.ExecuteAsync(listRequest, cancellationToken);

        if (requestResult.IsFailed)
            return requestResult.ToResult();

        var validationResult = ValidateResultCount(
            requestResult.Value.Files.Count,
            minResult,
            maxResult);

        return Result.Ok(requestResult.Value.Files)
              .WithReasons(validationResult.Reasons);
    }

    private static Result ValidateResultCount(int count, int? minResult, int? maxResult)
    {
        const string RANGE_ERROR = "Foi encontrado {0} resultados para os dados mencionados, quando era esperado no {1} {2}.";

        return count < minResult ? Result.Fail(string.Format(RANGE_ERROR, count, "mínimo", minResult))
             : count > maxResult ? Result.Fail(string.Format(RANGE_ERROR, count, "máximo", maxResult))
             : Result.Ok();
    }

    public virtual File CreateResourceObject(
        string mimeType,
        string name,
        string? parentId = null)
        => new()
        {
            Name = name,
            Description = name,
            MimeType = mimeType,
            Parents = [parentId ?? GoogleDriveSettingsService.BaseFolderId]
        };

    public virtual Task<Result<string>> DeleteResource(
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        var deleteRequest = driveService.Files.Delete(resourceId);
        return googleWrapper.ExecuteAsync(deleteRequest, cancellationToken);
    }
}
