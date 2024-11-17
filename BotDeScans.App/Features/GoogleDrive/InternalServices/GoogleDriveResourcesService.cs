using BotDeScans.App.Services.ExternalClients;
using FluentResults;
using File = Google.Apis.Drive.v3.Data.File;
namespace BotDeScans.App.Features.GoogleDrive.InternalServices;

public class GoogleDriveResourcesService(
    GoogleDriveClient googleDriveClient,
    GoogleDriveWrapper googleDriveWrapper)
{
    /// <summary>
    /// Get resource (file or folder) based on its name, mimetype and, optionally, parentId folder.
    /// It returns an error if more than one file is found based on matching criteria.
    /// </summary>
    /// <param name="mimeType">resource mimeType</param>
    /// <param name="name">mimeType name</param>
    /// <param name="parentId">parent folder id. If null, it will look at bot root folder.</param>
    /// <param name="cancellationToken">cancellationToken</param>
    /// <returns>Resource, if exists. Otherwise null</returns>
    public virtual async Task<Result<IList<File>>> GetResourcesAsync(
        string? mimeType,
        string? forbiddenMimeType,
        string? name,
        string? parentId,
        int? minResult,
        int? maxResult,
        CancellationToken cancellationToken = default)
    {
        // todo: move it to a queryBuilder
        var mimeTypeCondition = mimeType is null ? "" : $" and mimeType = '{mimeType}'";
        var forbiddenMimeTypeCondition = forbiddenMimeType is null ? "" : $" and mimeType != '{forbiddenMimeType}'";
        var nameCondition = name is null ? "" : $" and name = '{name}'";
        var parentCondition = $" and '{parentId ?? GoogleDriveSettingsService.BaseFolderId}' in parents";
        var query = @$"trashed = false{mimeTypeCondition}{forbiddenMimeTypeCondition}{nameCondition}{parentCondition}";

        var listRequest = googleDriveClient.Client.Files.List();
        listRequest.Q = query;
        var requestResult = await googleDriveWrapper.ExecuteAsync(listRequest, cancellationToken);

        if (requestResult.IsFailed)
            return requestResult.ToResult();

        if (minResult is not null && requestResult.Value.Files.Count < minResult)
            return Result.Fail($"Foi encontrado mais de um recurso para os dados mencionados, quando era esperado no mínimo {minResult}.");

        if (maxResult is not null && requestResult.Value.Files.Count > maxResult)
            return Result.Fail($"Foi encontrado mais de um recurso para os dados mencionados, quando era esperado no máximo {maxResult}.");

        return Result.Ok(requestResult.Value.Files);
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
            Parents = new[] { parentId ?? GoogleDriveSettingsService.BaseFolderId }
        };

    public virtual Task<Result<string>> DeleteResource(
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        var deleteRequest = googleDriveClient.Client.Files.Delete(resourceId);
        return googleDriveWrapper.ExecuteAsync(deleteRequest, cancellationToken);
    }
}
