using BotDeScans.App.Services.Wrappers;
using FluentResults;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
namespace BotDeScans.App.Features.GoogleDrive.InternalServices;

public class GoogleDrivePermissionsService(
    DriveService driveService,
    GoogleWrapper googleWrapper)
{
    public const string PUBLIC_PERMISSION_TYPE = "anyone";
    public const string USER_PERMISSION_TYPE = "user";
    public const string READER_ROLE = "reader";

    public virtual async Task<Result<IEnumerable<Permission>>> GetUserPermissionsAsync(
        string email,
        string resourceId,
        CancellationToken cancellationToken)
    {
        var permissionsRequest = driveService.Permissions.List(resourceId);
        permissionsRequest.Fields = "*";

        var permissionsResult = await googleWrapper.ExecuteAsync(permissionsRequest, cancellationToken);
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
        var createRequest = driveService.Permissions.Create(permission, resourceId);
        return googleWrapper.ExecuteAsync(createRequest, cancellationToken);
    }

    public virtual Task<Result<Permission>> CreateUserReaderPermissionAsync(
        string email,
        string resourceId,
        CancellationToken cancellationToken)
    {
        var permission = new Permission { Type = USER_PERMISSION_TYPE, Role = READER_ROLE, EmailAddress = email.ToLower() };
        var createRequest = driveService.Permissions.Create(permission, resourceId);
        return googleWrapper.ExecuteAsync(createRequest, cancellationToken);
    }

    public virtual async Task<Result> DeleteUserReaderPermissionsAsync(
        IEnumerable<Permission> permissions,
        string resourceId,
        CancellationToken cancellationToken)
    {
        var requests = permissions
            .Select(permission => driveService.Permissions.Delete(resourceId, permission.Id))
            .Select(async request => await googleWrapper.ExecuteAsync(request, cancellationToken));

        var results = await Task.WhenAll(requests);
        return Result.Merge(results).ToResult();
    }
}
