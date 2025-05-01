using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Initializations.ClientFactories;
using FluentValidation;

namespace BotDeScans.App.Services.Initializations.FactoriesValidators;

public class GoogleDriveClientFactoryValidator : AbstractValidator<GoogleDriveClientFactory>
{
    public GoogleDriveClientFactoryValidator()
    {
        var credentialResult = GoogleDriveClientFactory.ConfigFileExists(GoogleDriveClientFactory.CREDENTIALS_FILE_NAME);

        RuleFor(factory => factory)
            .Must(_ => credentialResult.IsSuccess)
            .WithMessage(credentialResult.ToValidationErrorMessage());
    }
}
