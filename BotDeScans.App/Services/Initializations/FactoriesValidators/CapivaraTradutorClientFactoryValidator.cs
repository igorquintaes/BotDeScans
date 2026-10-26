using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Initializations.Factories;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Services.Initializations.FactoriesValidators;

public class CapivaraTradutorClientFactoryValidator : AbstractValidator<CapivaraTradutorClientFactory>
{
    public CapivaraTradutorClientFactoryValidator(IConfiguration configuration)
    {
        var groupId = configuration.GetRequiredValueAsResult<string>("Capivara:GroupId");
        var apiKey = configuration.GetRequiredValueAsResult<string>("Capivara:ApiKey");

        RuleFor(factory => factory)
            .Must(_ => groupId.IsSuccess)
            .WithMessage(groupId.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => apiKey.IsSuccess)
            .WithMessage(apiKey.ToValidationErrorMessage());
    }
}
