using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Initializations.Factories;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Services.Initializations.FactoriesValidators;

public class BoxClientFactoryValidator : AbstractValidator<BoxClientFactory>
{
    public BoxClientFactoryValidator(IConfiguration configuration)
    {
        var clientId = configuration.GetRequiredValueAsResult<string>("Box:ClientId");
        var clientSecret = configuration.GetRequiredValueAsResult<string>("Box:ClientSecret");

        RuleFor(factory => factory)
            .Must(_ => clientId.IsSuccess)
            .WithMessage(clientId.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => clientSecret.IsSuccess)
            .WithMessage(clientSecret.ToValidationErrorMessage());
    }
}
