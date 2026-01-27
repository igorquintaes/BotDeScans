using BotDeScans.App.Services.Initializations.Factories;
using FluentValidation;

namespace BotDeScans.App.Services.Initializations.FactoriesValidators;

public class BoxClientFactoryValidator : AbstractValidator<BoxClientFactory>
{
    public BoxClientFactoryValidator()
    {
        // todo: verify credentials in IConfiguration

    }
}
