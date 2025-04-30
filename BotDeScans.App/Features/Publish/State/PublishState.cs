using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.Pings;
using BotDeScans.App.Features.Publish.State.Models;
using BotDeScans.App.Models;
using BotDeScans.App.Services.Discord;
using FluentValidation;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Features.Publish.State;

public class PublishState
{
    public EnabledSteps Steps { get; set; } = null!;
    public Title Title { get; set; } = null!;
    public Info ReleaseInfo { get; set; } = null!;
    public Links ReleaseLinks { get; set; } = new();
    public InternalData InternalData { get; set; } = new();
}

public class PublishStateValidator : AbstractValidator<PublishState>
{
    public PublishStateValidator(
        RolesService rolesService,
        IConfiguration configuration,
        IValidator<Info> infoValidator,
        IValidator<Title> titleValidator)
    {
        RuleFor(model => model.ReleaseInfo)
            .SetValidator(infoValidator);

        RuleFor(model => model.Title)
            .Must(prop => prop.References.Any(reference => reference.Key == ExternalReference.MangaDex))
            .When(prop => prop.Steps.Any(step => step.Key.Name == Steps.Enums.StepName.UploadMangadex))
            .WithMessage("Não foi definida uma referência para a publicação da obra na MangaDex.")
            .SetValidator(titleValidator);

        var globalPingValue = configuration.GetValue<string?>(GlobalPing.GLOBAL_ROLE_KEY, null);
        var pingTypeAsString = configuration.GetValue<string?>(Ping.PING_TYPE_KEY, null);
        var isPingTypeValid = Enum.TryParse<PingType>(pingTypeAsString, out var pingType);

        RuleFor(model => model)
            .Cascade(CascadeMode.Stop)
            .Must(_ => string.IsNullOrWhiteSpace(globalPingValue) is false)
            .When(_ => pingType == PingType.Global)
            .WithMessage("É necessário definir um valor para ping global no arquivo de configuração do Bot de Scans.")
            .MustAsync(async (_, prop, context, cancellationToken) =>
            {
                var rolesResult = await rolesService.GetRoleFromGuildAsync(globalPingValue!, cancellationToken);
                if (rolesResult.IsSuccess)
                    return true;

                context.AddFailure(string.Join("; ", rolesResult.Errors.Select(error => error.Message)));
                return false;
            })
            .When(prop => string.IsNullOrWhiteSpace(globalPingValue) is false && pingType is PingType.Global);
    }
}