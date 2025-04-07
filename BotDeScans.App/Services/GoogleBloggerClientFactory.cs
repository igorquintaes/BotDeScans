using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Services.Wrappers;
using FluentResults;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Blogger.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Services;

public class GoogleBloggerClientFactory(
    IConfiguration configuration,
    GoogleWrapper googleWrapper)
    : ClientFactory<BloggerService>
{
    public const string CREDENTIALS_FILE_NAME = "blogger.json";

    public override bool ExpectedInPublishFeature => configuration
        .GetRequiredValues<StepEnum>("Settings:Publish:Steps", value => Enum.Parse(typeof(StepEnum), value))
        .Any(x => x == StepEnum.PublishBlogspot);

    public override Result ValidateConfiguration()
    {
        var aggregatedResult = Result.Ok();

        var bloggerId = configuration.GetValue<string?>("Blogger:Id");
        if (string.IsNullOrWhiteSpace(bloggerId))
            aggregatedResult = aggregatedResult.WithError($"'Blogger:Id': value not found in config.json.");

        var bloggerUrl = configuration.GetValue<string?>("Blogger:Url");
        if (string.IsNullOrWhiteSpace(bloggerUrl))
            aggregatedResult = aggregatedResult.WithError($"'Blogger:Url': value not found in config.json.");

        if (string.IsNullOrWhiteSpace(bloggerUrl) is false 
            && Uri.TryCreate(bloggerUrl, UriKind.Absolute, out var _) is false)
            aggregatedResult = aggregatedResult.WithError("Não foi possível identificar o link do Blogger como válido.");
        
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var templateFileName = GoogleBloggerService.TEMPLATE_FILE_NAME;
        var templateFilePath = Path.Combine(baseDirectory, "config", templateFileName);
        if (!File.Exists(templateFilePath))
            aggregatedResult = aggregatedResult.WithError($"Não foi possível encontrar o arquivo de template : {templateFileName}");

        var credentialResult = ConfigFileExists(CREDENTIALS_FILE_NAME);
        aggregatedResult = aggregatedResult.WithReasons(credentialResult.Reasons);

        var templateResult = ConfigFileExists(GoogleBloggerService.TEMPLATE_FILE_NAME);
        aggregatedResult = aggregatedResult.WithReasons(templateResult.Reasons);

        return aggregatedResult;
    }

    public override async Task<Result<BloggerService>> CreateAsync(
        CancellationToken cancellationToken = default)
    {
        await using var credentialStream = GetConfigFileAsStream(CREDENTIALS_FILE_NAME).Value;

        var saveAccessTokenPath = Path.Combine("config", "tokens");
        var clientSecrets = await GoogleClientSecrets.FromStreamAsync(credentialStream, cancellationToken);
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            clientSecrets: clientSecrets.Secrets,
            scopes: [BloggerService.Scope.Blogger],
            user: "user",
            cancellationToken,
            dataStore: new FileDataStore(saveAccessTokenPath, true));

        return new BloggerService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "BotDeScans",
        });
    }

    public override async Task<Result> HealthCheckAsync(BloggerService client, CancellationToken cancellationToken)
    {
        var bloggerId = configuration.GetRequiredValue<string>("Blogger:Id");
        var listRequest = client.Posts.List(bloggerId!);
        listRequest.MaxResults = 1;

        var listResult = await googleWrapper.ExecuteAsync(listRequest, cancellationToken);
        return listResult.ToResult();
    }
}
