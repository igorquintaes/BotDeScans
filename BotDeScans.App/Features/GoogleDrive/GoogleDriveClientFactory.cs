using BotDeScans.App.Extensions;
using BotDeScans.App.Services;
using BotDeScans.App.Services.Wrappers;
using FluentResults;
using FluentValidation;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
namespace BotDeScans.App.Features.GoogleDrive;

public class GoogleDriveClientFactory(GoogleWrapper googleWrapper) : ClientFactory<DriveService>
{
    public const string CREDENTIALS_FILE_NAME = "googledrive.json";

    public override bool ExpectedInPublishFeature => true;

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

public class GoogleDriveClientFactoryValidator : AbstractValidator<GoogleDriveClientFactory>
{
    public GoogleDriveClientFactoryValidator()
    {
        var credentialResult = GoogleDriveClientFactory.ConfigFileExists(GoogleDriveClientFactory.CREDENTIALS_FILE_NAME);

        RuleFor(factory => factory)
            .Must(_ => credentialResult.IsSuccess)
            .WithMessage(credentialResult.ToValidationErrorMessage());
    }
}