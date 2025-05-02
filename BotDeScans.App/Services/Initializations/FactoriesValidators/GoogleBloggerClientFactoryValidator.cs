using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Initializations.Factories;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Services.Initializations.FactoriesValidators;

public class GoogleBloggerClientFactoryValidator : AbstractValidator<GoogleBloggerClientFactory>
{
    public GoogleBloggerClientFactoryValidator(IConfiguration configuration)
    {
        var bloggerIdResult = configuration.GetRequiredValueAsResult<string>("Blogger:Id");
        var bloggerUrlResult = configuration.GetRequiredValueAsResult<string>("Blogger:Url");

        RuleFor(factory => factory)
            .Must(_ => bloggerIdResult.IsSuccess)
            .WithMessage(bloggerIdResult.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => bloggerUrlResult.IsSuccess)
            .WithMessage(bloggerUrlResult.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => Uri.TryCreate(bloggerUrlResult.Value, UriKind.Absolute, out var _))
            .WithMessage("Não foi possível identificar o link do Blogger como válido.")
            .When(_ => bloggerUrlResult.IsSuccess);

        var credentialResult = GoogleBloggerClientFactory.ConfigFileExists(GoogleBloggerClientFactory.CREDENTIALS_FILE_NAME);
        var templateResult = GoogleBloggerClientFactory.ConfigFileExists(GoogleBloggerService.TEMPLATE_FILE_NAME);

        RuleFor(factory => factory)
            .Must(_ => credentialResult.IsSuccess)
            .WithMessage(credentialResult.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => templateResult.IsSuccess)
            .WithMessage(templateResult.ToValidationErrorMessage());
    }
}
