using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps;
using FluentResults;
using MangaDexSharp;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Services;

public class MangaDexClientTokenFactory(
    IMangaDex mangaDex,
    IConfiguration configuration)
    : ClientFactory<MangaDexAccessToken>
{
    public override bool ExpectedInPublishFeature => configuration
        .GetRequiredValues<StepEnum>("Settings:Publish:Steps", value => Enum.Parse(typeof(StepEnum), value))
        .Any(x => x == StepEnum.UploadMangadex);

    public override Result ValidateConfiguration()
    {
        var aggregatedResult = Result.Ok();

        var username = configuration.GetValue<string?>("Mangadex:Username");
        if (string.IsNullOrWhiteSpace(username))
            aggregatedResult = aggregatedResult.WithError("'Mangadex:Username': value not found in config.json.");

        var password = configuration.GetValue<string?>("Mangadex:Password");
        if (string.IsNullOrWhiteSpace(password))
            aggregatedResult = aggregatedResult.WithError("'Mangadex:Password': value not found in config.json.");

        var clientId = configuration.GetValue<string?>("Mangadex:ClientId");
        if (string.IsNullOrWhiteSpace(clientId))
            aggregatedResult = aggregatedResult.WithError("'Mangadex:ClientId': value not found in config.json.");

        var clientSecret = configuration.GetValue<string?>("Mangadex:ClientSecret");
        if (string.IsNullOrWhiteSpace(clientSecret))
            aggregatedResult = aggregatedResult.WithError("'Mangadex:ClientSecret': value not found in config.json.");

        var groupId = configuration.GetValue<string?>("Mangadex:GroupId");
        if (string.IsNullOrWhiteSpace(groupId))
            aggregatedResult = aggregatedResult.WithError("'Mangadex:GroupId': value not found in config.json.");

        return aggregatedResult;
    }

    public override async Task<Result<MangaDexAccessToken>> CreateAsync(CancellationToken cancellationToken)
    {
        var username = configuration.GetRequiredValue<string>("Mangadex:Username");
        var password = configuration.GetRequiredValue<string>("Mangadex:Password");
        var clientId = configuration.GetRequiredValue<string>("Mangadex:ClientId");
        var clientSecret = configuration.GetRequiredValue<string>("Mangadex:ClientSecret");

        var result = await mangaDex.Auth.Personal(clientId, clientSecret, username, password);

        if (result is null ||
            result.ExpiresIn is null ||
            result.ExpiresIn <= 0 ||
            string.IsNullOrWhiteSpace(result.AccessToken))
            return Result.Fail("Unable to login in mangadex.");

        return new MangaDexAccessToken(result.AccessToken);
    }

    public override async Task<Result> HealthCheckAsync(MangaDexAccessToken accessToken, CancellationToken cancellationToken)
    {
        var me = await mangaDex.User.Me(accessToken.Value);
        if (me.ErrorOccurred)
        {
            var errors = me.Errors.Select(x => $"{x.Status} {x.Title}: {x.Detail}");
            return Result.Fail(errors);
        }

        return Result.Ok();
    }
}

public class MangaDexAccessToken(string value)
{
    public string Value { get; } = value;
}
