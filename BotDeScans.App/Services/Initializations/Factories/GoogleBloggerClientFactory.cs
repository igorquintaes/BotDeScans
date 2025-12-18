using BotDeScans.App.Extensions;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services.Initializations.Factories.Base;
using BotDeScans.App.Services.Wrappers;
using FluentResults;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Blogger.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Services.Initializations.Factories;

public class GoogleBloggerClientFactory(
    IConfiguration configuration,
    GoogleWrapper googleWrapper)
    : ClientFactory<BloggerService>
{
    public const string CREDENTIALS_FILE_NAME = "blogger.json";

    public override bool Enabled => configuration
        .GetValues<StepName>("Settings:Publish:Steps")
        .Any(x => x == StepName.PublishBlogspot);

    public override async Task<Result<BloggerService>> CreateAsync(
        CancellationToken cancellationToken = default)
    {
        var saveAccessTokenPath = Path.Combine("config", "tokens");

        var credentialStreamResult = GetConfigFileAsStream(CREDENTIALS_FILE_NAME);
        if (credentialStreamResult.IsFailed)
            return credentialStreamResult.ToResult();

        await using var credentialStream = credentialStreamResult.Value;
        var clientSecrets = await GoogleClientSecrets.FromStreamAsync(credentialStream, cancellationToken);

        var credential = await GetUserCredential(clientSecrets, saveAccessTokenPath, cancellationToken);

        return new BloggerService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "BotDeScans",
        });
    }

    public override async Task<Result> HealthCheckAsync(BloggerService client, CancellationToken cancellationToken)
    {
        var bloggerId = configuration.GetRequiredValue<string>("Blogger:Id");
        var listRequest = client.Posts.List(bloggerId);
        listRequest.MaxResults = 1;

        var listResult = await googleWrapper.ExecuteAsync(listRequest, cancellationToken);
        return listResult.ToResult();
    }

    [ExcludeFromCodeCoverage(Justification =
        "This static method needs a browser window interaction to proceed." +
        "ref: https://github.com/googleapis/google-api-dotnet-client/blob/306a2a14a19f4763c0fb89a7b351fa1600bbaf25/Src/Support/Google.Apis.IntegrationTests/GoogleWebAuthorizationBrokerTests.cs#L36")]
    protected virtual Task<UserCredential> GetUserCredential(
        GoogleClientSecrets clientSecrets,
        string saveAccessTokenPath,
        CancellationToken cancellationToken) =>
        GoogleWebAuthorizationBroker.AuthorizeAsync(
            clientSecrets: clientSecrets.Secrets,
            scopes: [BloggerService.Scope.Blogger],
            user: "BotDeScans",
            cancellationToken,
            dataStore: new FileDataStore(saveAccessTokenPath, true));
}
