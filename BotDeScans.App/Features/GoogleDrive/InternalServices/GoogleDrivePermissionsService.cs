using BotDeScans.App.Services.ExternalClients;
using FluentResults;
using Google.Apis.Drive.v3.Data;
namespace BotDeScans.App.Features.GoogleDrive.InternalServices;

public class GoogleDrivePermissionsService(
    GoogleDriveClient googleDriveClient,
    GoogleDriveWrapper googleDriveWrapper)
{
    public virtual Task<Result<Permission>> CreatePublicReaderPermissionAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var permission = new Permission { Type = "anyone", Role = "reader" };
        var createRequest = googleDriveClient.Client.Permissions.Create(permission, id);
        return googleDriveWrapper.ExecuteAsync(createRequest, cancellationToken);
    }

    public virtual async Task<Result<IEnumerable<Permission>>> GetDriverAccessPermissionsAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        // Value based on Google Docs: https://developers.google.com/drive/api/v2/reference/permissions/list
        // Is expected that Base Folder does not surpass 100 permissions...
        // If users surpass this threshold, we can't consider it as bug... They're the bug!
        const int MAX_PERMISSIONS_PER_REQUEST = 100;

        var permissionsRequest = googleDriveClient.Client.Permissions.List(GoogleDriveSettingsService.BaseFolderId);
        permissionsRequest.Fields = "*";
        permissionsRequest.PageSize = MAX_PERMISSIONS_PER_REQUEST;

        var permissionsResult = await googleDriveWrapper.ExecuteAsync(permissionsRequest, cancellationToken);
        return permissionsResult.IsSuccess
            ? Result.Ok(permissionsResult.Value.Permissions.Where(x =>
                x.EmailAddress.Equals(email, StringComparison.InvariantCultureIgnoreCase) &&
                x.Type.Equals("user", StringComparison.InvariantCultureIgnoreCase)))
            : permissionsResult.ToResult();
    }

    public virtual Task<Result<Permission>> CreateBaseUserReaderPermissionAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var permission = new Permission { Type = "user", Role = "reader", EmailAddress = email.ToLower() };
        var createRequest = googleDriveClient.Client.Permissions.Create(permission, GoogleDriveSettingsService.BaseFolderId);
        return googleDriveWrapper.ExecuteAsync(createRequest, cancellationToken);
    }

    public virtual async Task<Result> DeleteBaseUserPermissionsAsync(
        IEnumerable<Permission> permissions,
        CancellationToken cancellationToken = default)
    {
        var returnResult = Result.Ok();
        foreach (var permissionToDelete in permissions)
        {
            var deleteRequest = googleDriveClient.Client.Permissions.Delete(GoogleDriveSettingsService.BaseFolderId, permissionToDelete.Id);
            var deleteResult = await googleDriveWrapper.ExecuteAsync(deleteRequest, cancellationToken);
            if (deleteResult.IsFailed)
                returnResult.WithErrors(deleteResult.Errors);
        }

        return returnResult;
    }
}
