using BotDeScans.App.Services.ExternalClients;
using FluentResults;
using Google.Apis.Drive.v3.Data;
namespace BotDeScans.App.Features.GoogleDrive.InternalServices;

public class GoogleDrivePermissionsService(
    GoogleDriveClient googleDriveClient,
    GoogleDriveWrapper googleDriveWrapper)
{
    public const string PUBLIC_PERMISSION_TYPE = "anyone";
    public const string USER_PERMISSION_TYPE = "user";
    public const string READER_ROLE = "reader";

    public virtual async Task<Result<IEnumerable<Permission>>> GetUserPermissionsAsync(
        string email,
        string resourceId,
        CancellationToken cancellationToken)
    {
        var permissionsRequest = googleDriveClient.Client.Permissions.List(resourceId);
        permissionsRequest.Fields = "*";

        var permissionsResult = await googleDriveWrapper.ExecuteAsync(permissionsRequest, cancellationToken);
        if (permissionsResult.IsFailed)
            return permissionsResult.ToResult();

        return Result.Ok(permissionsResult.Value.Permissions.Where(x =>
            x.EmailAddress.Equals(email, StringComparison.InvariantCultureIgnoreCase) &&
            x.Type.Equals(USER_PERMISSION_TYPE, StringComparison.InvariantCultureIgnoreCase)));
    }

    public virtual Task<Result<Permission>> CreatePublicReaderPermissionAsync(
        string resourceId,
        CancellationToken cancellationToken)
    {
        var permission = new Permission { Type = PUBLIC_PERMISSION_TYPE, Role = READER_ROLE };
        var createRequest = googleDriveClient.Client.Permissions.Create(permission, resourceId);
        return googleDriveWrapper.ExecuteAsync(createRequest, cancellationToken);
    }

    public virtual Task<Result<Permission>> CreateUserReaderPermissionAsync(
        string email,
        string resourceId,
        CancellationToken cancellationToken)
    {
        var permission = new Permission { Type = USER_PERMISSION_TYPE, Role = READER_ROLE, EmailAddress = email.ToLower() };
        var createRequest = googleDriveClient.Client.Permissions.Create(permission, resourceId);
        return googleDriveWrapper.ExecuteAsync(createRequest, cancellationToken);
    }

    public virtual async Task<Result> DeleteUserReaderPermissionsAsync(
        IEnumerable<Permission> permissions,
        string resourceId,
        CancellationToken cancellationToken)
    {
        var requests = permissions
            .Select(permission => googleDriveClient.Client.Permissions.Delete(resourceId, permission.Id))
            .Select(async request => await googleDriveWrapper.ExecuteAsync(request, cancellationToken));

        var results = await Task.WhenAll(requests);
        return Result.Merge(results).ToResult();
    }
}
