using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Initializations.Factories;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Services.Initializations.FactoriesValidators;

public class SakuraMangasClientFactoryValidator : AbstractValidator<SakuraMangasClientFactory>
{
    public SakuraMangasClientFactoryValidator(IConfiguration configuration)
    {
        var userResult = configuration.GetRequiredValueAsResult<string>("SakuraMangas:User");
        var passResult = configuration.GetRequiredValueAsResult<string>("SakuraMangas:Pass");

        RuleFor(factory => factory)
            .Must(_ => userResult.IsSuccess)
            .WithMessage(userResult.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => passResult.IsSuccess)
            .WithMessage(passResult.ToValidationErrorMessage());
    }
}
