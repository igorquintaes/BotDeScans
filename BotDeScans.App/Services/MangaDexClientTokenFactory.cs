using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps.Enums;
using FluentResults;
using FluentValidation;
using MangaDexSharp;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;

namespace BotDeScans.App.Services;

public class MangaDexClientTokenFactory(
    IMangaDex mangaDex,
    IConfiguration configuration)
    : ClientFactory<MangaDexAccessToken>
{
    public override bool ExpectedInPublishFeature => configuration
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

public class MangaDexClientTokenFactoryValidator : AbstractValidator<MangaDexClientTokenFactory>
{
    public MangaDexClientTokenFactoryValidator(IConfiguration configuration)
    {
        var groupIdResult = configuration.GetRequiredValueAsResult<string>("Mangadex:GroupId");
        var usernameResult = configuration.GetRequiredValueAsResult<string>("Mangadex:Username");
        var passwordResult = configuration.GetRequiredValueAsResult<string>("Mangadex:Password");
        var clientIdResult = configuration.GetRequiredValueAsResult<string>("Mangadex:ClientId");
        var clientSecredResult = configuration.GetRequiredValueAsResult<string>("Mangadex:ClientSecret");

        RuleFor(factory => factory)
            .Must(_ => groupIdResult.IsSuccess)
            .WithMessage(groupIdResult.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => usernameResult.IsSuccess)
            .WithMessage(usernameResult.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => passwordResult.IsSuccess)
            .WithMessage(passwordResult.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => clientIdResult.IsSuccess)
            .WithMessage(clientIdResult.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => clientSecredResult.IsSuccess)
            .WithMessage(clientSecredResult.ToValidationErrorMessage());
    }
}
