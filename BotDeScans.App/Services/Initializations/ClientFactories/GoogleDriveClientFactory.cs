using BotDeScans.App.Services.Initializations.ClientFactories.Base;
using BotDeScans.App.Services.Wrappers;
using FluentResults;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
namespace BotDeScans.App.Services.Initializations.ClientFactories;

public class GoogleDriveClientFactory(GoogleWrapper googleWrapper) : ClientFactory<DriveService>
{
    public const string CREDENTIALS_FILE_NAME = "googledrive.json";

    public override bool Enabled => true;

    public override async Task<Result<DriveService>> CreateAsync(CancellationToken cancellationToken = default)
    {
        await using var credentialStream = GetConfigFileAsStream(CREDENTIALS_FILE_NAME).Value;
        var credential = await GoogleCredential.FromStreamAsync(credentialStream, cancellationToken);
        return new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential.CreateScoped(DriveService.Scope.Drive).UnderlyingCredential,
            ApplicationName = "BotDeScans"
        });
    }

    public override async Task<Result> HealthCheckAsync(DriveService client, CancellationToken cancellationToken)
    {
        var listRequest = client.Files.List();
        listRequest.PageSize = 1;
        listRequest.Q = "'root' in parents";

        var listResult = await googleWrapper.ExecuteAsync(listRequest, cancellationToken);
        return listResult.ToResult();
    }
}