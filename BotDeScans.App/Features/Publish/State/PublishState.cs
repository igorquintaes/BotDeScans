using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.Pings;
using BotDeScans.App.Features.Publish.State.Models;
using BotDeScans.App.Features.Publish.Steps;
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
            .When(prop => prop.Steps.Any(step => step.Key is UploadMangaDexStep))
            .WithMessage("Não foi definida uma referência para a publicação da obra na MangaDex.")
            .SetValidator(titleValidator);

        RuleFor(model => model.Title)
            .Must(prop => prop.References.Any(reference => reference.Key == ExternalReference.MangaDex))
            .When(prop => prop.Steps.Any(step => step.Key is UploadSakuraMangasStep))
            .WithMessage("Não foi definida uma referência para a publicação da obra na Sakura Mangás. (Mesma referência que a MangaDex)")
            .SetValidator(titleValidator);

        var globalPingValue = configuration.GetValue<string?>(GlobalPing.GLOBAL_ROLE_KEY, null);
        var pingTypeAsString = configuration.GetValue<string?>(Ping.PING_TYPE_KEY, null);
        var isPingTypeValid = Enum.TryParse<PingType>(pingTypeAsString, out var pingType);

        RuleFor(model => model)
            .Cascade(CascadeMode.Stop)
            .Must(_ => string.IsNullOrWhiteSpace(globalPingValue) is false)
            .When(_ => pingType == PingType.Global)
            .WithMessage("É necessário definir um valor para ping global no arquivo de configuração do Bot de Scans.")
            .DependentRules(() =>
            {
                RuleFor(model => model)
                    .MustAsync(async (_, prop, context, cancellationToken) => await RoleMustExists(globalPingValue!, rolesService, context, cancellationToken))
                    .When(prop => pingType is PingType.Global);
            });
    }

    private static async Task<bool> RoleMustExists(
        string role,
        RolesService rolesService,
        ValidationContext<PublishState> context,
        CancellationToken cancellationToken)
    {
        var rolesResult = await rolesService.GetRoleFromGuildAsync(role, cancellationToken);
        if (rolesResult.IsSuccess)
            return true;

        context.AddFailure(string.Join("; ", rolesResult.Errors.Select(error => error.Message)));
        return false;
    }
}