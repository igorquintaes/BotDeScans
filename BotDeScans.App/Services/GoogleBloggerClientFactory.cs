using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services.Wrappers;
using FluentResults;
using FluentValidation;
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
        .GetRequiredValues<StepName>("Settings:Publish:Steps", value => Enum.Parse(typeof(StepName), value))
        .Any(x => x == StepName.PublishBlogspot);

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

public class GoogleBloggerClientFactoryValidator : AbstractValidator<GoogleBloggerClientFactory>
{
    public GoogleBloggerClientFactoryValidator(IConfiguration configuration)
    {
        var bloggerIdResult = configuration.GetRequiredValueAsResult<string>("Blogger:Id");
        var bloggerUrlResult = configuration.GetRequiredValueAsResult<string>("Blogger:Url");

        RuleFor(factory => factory)
            .Must(_ => bloggerIdResult.IsSuccess)
            .WithMessage(bloggerIdResult.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => bloggerUrlResult.IsSuccess)
            .WithMessage(bloggerUrlResult.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => Uri.TryCreate(bloggerUrlResult.Value, UriKind.Absolute, out var _))
            .WithMessage("Não foi possível identificar o link do Blogger como válido.")
            .When(_ => bloggerUrlResult.IsSuccess);

        var credentialResult = GoogleBloggerClientFactory.ConfigFileExists(GoogleBloggerClientFactory.CREDENTIALS_FILE_NAME);
        var templateResult = GoogleBloggerClientFactory.ConfigFileExists(GoogleBloggerService.TEMPLATE_FILE_NAME);

        RuleFor(factory => factory)
            .Must(_ => credentialResult.IsSuccess)
            .WithMessage(credentialResult.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => templateResult.IsSuccess)
            .WithMessage(templateResult.ToValidationErrorMessage());
    }
}