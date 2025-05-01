using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services;
using CG.Web.MegaApiClient;
using FluentResults;
using FluentValidation;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Features.Mega;

public class MegaClientFactory(IConfiguration configuration) : ClientFactory<IMegaApiClient>
{
    public override bool ExpectedInPublishFeature => configuration
        .GetRequiredValues<StepName>("Settings:Publish:Steps", value => Enum.Parse(typeof(StepName), value))
        .Any(x => x is StepName.UploadPdfMega or StepName.UploadZipMega);

    public override async Task<Result<IMegaApiClient>> CreateAsync(CancellationToken cancellationToken)
    {
        var user = configuration.GetRequiredValue<string>("Mega:User");
        var pass = configuration.GetRequiredValue<string>("Mega:Pass");
        var client = new MegaApiClient();

        await client.LoginAsync(user, pass);

        return client.IsLoggedIn is false
            ? Result.Fail("Unable to login on Mega. Check your user and pass, or if your account is blocked.")
            : Result.Ok<IMegaApiClient>(client);
    }

    public override async Task<Result> HealthCheckAsync(IMegaApiClient client, CancellationToken cancellationToken)
    {
        var accInfo = await client.GetAccountInformationAsync();
        return Result.OkIf(accInfo is not null, "Error while trying to retrieve information from account.");
    }
}

public class MegaClientFactoryValidator : AbstractValidator<MegaClientFactory>
{
    public MegaClientFactoryValidator(IConfiguration configuration)
    {
        var userResult = configuration.GetRequiredValueAsResult<string>("Mega:User");
        var passResult = configuration.GetRequiredValueAsResult<string>("Mega:Pass");

        RuleFor(factory => factory)
            .Must(_ => userResult.IsSuccess)
            .WithMessage(userResult.ToValidationErrorMessage());

        RuleFor(factory => factory)
            .Must(_ => passResult.IsSuccess)
            .WithMessage(passResult.ToValidationErrorMessage());
    }
}
