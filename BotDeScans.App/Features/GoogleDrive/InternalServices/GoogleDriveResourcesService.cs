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
    public virtual async Task<Result<File?>> GetResourceByNameAsync(
        string mimeType,
        string name,
        string? parentId,
        CancellationToken cancellationToken = default)
    {
        // TODO: create query builder
        var query = @$"
                mimeType = '{mimeType}'
                and name = '{name}' 
                and trashed = false
                and '{parentId ?? GoogleDriveSettingsService.BaseFolderId}' in parents";

        var listRequest = googleDriveClient.Client.Files.List();
        listRequest.PageSize = 2;
        listRequest.Q = query;
        var requestResult = await googleDriveWrapper.ExecuteAsync(listRequest, cancellationToken);

        if (requestResult.IsFailed)
            return requestResult.ToResult();

        if (requestResult.Value.Files.Count > 1)
            return Result.Fail("Foi encontrado mais de um recurso para os dados mencionados, quando era esperado apenas um.");

        return Result.Ok(requestResult.Value.Files.SingleOrDefault());
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
