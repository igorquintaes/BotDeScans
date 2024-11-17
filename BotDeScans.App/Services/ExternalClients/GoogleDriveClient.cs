using BotDeScans.App.Features.GoogleDrive.InternalServices;
using FluentResults;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
namespace BotDeScans.App.Services.ExternalClients;

public class GoogleDriveClient(GoogleDriveWrapper googleDriveWrapper) : ExternalClientBase<DriveService>
{
    protected override bool Enabled => true;

    public override async Task<Result> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (Client is not null)
            return Result.Ok();

        try
        {
            var credentialResult = GetCredentialsAsStream("googledrive.json");
            if (credentialResult.IsFailed)
                return credentialResult.ToResult();

            await using var stream = credentialResult.Value;
            var credential = await GoogleCredential.FromStreamAsync(stream, cancellationToken);
            Client = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential.CreateScoped(DriveService.Scope.Drive).UnderlyingCredential,
                ApplicationName = "BotDeScans"
            });

            var listRequest = Client.Files.List();
            listRequest.PageSize = 1;
            listRequest.Q = "'root' in parents";

            var listResult = await googleDriveWrapper.ExecuteAsync(listRequest, cancellationToken);
            return listResult.ToResult();
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Error while creating GoogleDrive client.")
                         .CausedBy(ex));
        }
    }
}
