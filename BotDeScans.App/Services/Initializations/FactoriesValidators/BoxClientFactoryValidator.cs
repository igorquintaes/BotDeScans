using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Initializations.Factories;
using FluentValidation;

namespace BotDeScans.App.Services.Initializations.FactoriesValidators;

public class BoxClientFactoryValidator : AbstractValidator<BoxClientFactory>
{
    public BoxClientFactoryValidator()
    {
        var credentialResult = BoxClientFactory.ConfigFileExists(BoxClientFactory.CREDENTIALS_FILE_NAME);

        RuleFor(factory => factory)
            .Must(_ => credentialResult.IsSuccess)
            .WithMessage(credentialResult.ToValidationErrorMessage());
    }
}
