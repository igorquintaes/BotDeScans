using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Initializations.Factories;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Services.Initializations.FactoriesValidators;

public class MangaDexClientFactoryValidator : AbstractValidator<MangaDexClientFactory>
{
    public MangaDexClientFactoryValidator(IConfiguration configuration)
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

