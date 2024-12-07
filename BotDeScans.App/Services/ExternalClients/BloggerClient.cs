using BotDeScans.App.Extensions;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Features.Publish.Steps;
using FluentResults;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Blogger.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Services.ExternalClients;

public class BloggerClient(IConfiguration configuration, GoogleDriveWrapper googleDriveWrapper) : ExternalClientBase<BloggerService>
{
    protected override bool Enabled => configuration
        .GetRequiredValues<StepEnum>("Settings:Publish:Steps", value => Enum.Parse(typeof(StepEnum), value))
        .Any(x => x == StepEnum.PublishBlogspot);

    public override async Task<Result> InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (Client is not null || !Enabled)
            return Result.Ok();

        try
        {
            var bloggerId = configuration.GetValue<string?>("Blogger:Id");
            if (string.IsNullOrWhiteSpace(bloggerId)) return Result.Fail($"'Blogger:Id' config.json value not found.");

            var bloggerUrl = configuration.GetValue<string?>("Blogger:Url");
            if (string.IsNullOrWhiteSpace(bloggerUrl)) return Result.Fail($"'Blogger:Url' config.json value not found.");

            var credentialResult = GetCredentialsAsStream("blogger.json");
            if (credentialResult.IsFailed) return credentialResult.ToResult();

            await using var stream = credentialResult.Value;
            var credentialPath = Path.Combine("config", "tokens");
            var clients = await GoogleClientSecrets.FromStreamAsync(stream, cancellationToken);
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clients.Secrets,
                [BloggerService.Scope.Blogger],
                "user",
                cancellationToken,
                new FileDataStore(credentialPath, true));

            Client = new BloggerService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "BotDeScans",
            });

            var listRequest = Client.Posts.List(bloggerId!);
            listRequest.MaxResults = 1;

            var listResult = await googleDriveWrapper.ExecuteAsync(listRequest, cancellationToken);
            return listResult.ToResult();
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Unable to create Blogger client.")
                         .CausedBy(ex));
        }
    }
}
