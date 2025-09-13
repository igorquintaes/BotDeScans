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
        var bloggerCoverWidthResult = configuration.GetRequiredValueAsResult<string>("Blogger:Cover:Width");
        var bloggerCoverHeightResult = configuration.GetRequiredValueAsResult<string>("Blogger:Cover:Height");

        RuleFor(factory => factory)
            .Must(_ => bloggerIdResult.IsSuccess)
            .WithMessage(bloggerIdResult.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => bloggerUrlResult.IsSuccess)
            .WithMessage(bloggerUrlResult.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => bloggerCoverWidthResult.IsSuccess)
            .WithMessage(bloggerCoverWidthResult.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => bloggerCoverHeightResult.IsSuccess)
            .WithMessage(bloggerCoverHeightResult.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => int.TryParse(bloggerCoverWidthResult.Value, out var width) && width > 0)
            .When(_ => bloggerCoverWidthResult.IsSuccess)
            .WithMessage("A capa precisa ter um número válido de largura e superior a 0.");

        RuleFor(factory => factory)
            .Must(_ => int.TryParse(bloggerCoverHeightResult.Value, out var height) && height > 0)
            .When(_ => bloggerCoverHeightResult.IsSuccess)
            .WithMessage("A capa precisa ter um número válido de altura e superior a 0.");

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
