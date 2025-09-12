using BotDeScans.App.Extensions;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services.Initializations.Factories.Base;
using FluentResults;
using MangaDexSharp;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;

namespace BotDeScans.App.Services.Initializations.Factories;

public class MangaDexClientFactory(
    IMangaDex mangaDex,
    IConfiguration configuration)
    : ClientFactory<MangaDexAccessToken>
{
    public override bool Enabled => configuration
        .GetRequiredValues<StepName>("Settings:Publish:Steps", value => Enum.Parse(typeof(StepName), value))
        .Any(x => x == StepName.UploadMangadex);

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
