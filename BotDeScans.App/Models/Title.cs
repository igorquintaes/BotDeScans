using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.Pings;
using BotDeScans.App.Services.Discord;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
namespace BotDeScans.App.Models;

public record Title(string Name, ulong? DiscordRoleId)
{
    public int Id { get; init; }
    public List<TitleReference> References { get; init; } = [];
}

public class TitleValidator : AbstractValidator<Title>
{
    public TitleValidator(
        RolesService rolesService,
        IConfiguration configuration)
    {
        var pingTypeAsString = configuration.GetRequiredValue<string>(Ping.PING_TYPE_KEY);
        var pingType = Enum.Parse<PingType>(pingTypeAsString);

        RuleFor(model => model.DiscordRoleId)
            .Cascade(CascadeMode.Stop)
            .Must(prop => prop.HasValue && prop != default(ulong))
            .When(prop => pingType is PingType.Global or PingType.Role)
            .WithMessage($"Não foi definida uma role para o Discord nesta obra, obrigatória para o ping de tipo {pingType}. " +
                          "Defina, ou mude o tipo de ping para publicação no arquivo de configuração do Bot de Scans.")
            .DependentRules(() =>
            {
                RuleFor(model => model.DiscordRoleId)
                    .MustAsync(async (_, prop, context, cancellationToken) => await RoleMustExists(prop!.Value, rolesService, context, cancellationToken))
                    .When(prop => prop.DiscordRoleId.HasValue &&
                                  prop.DiscordRoleId != default(ulong) &&
                                  pingType is PingType.Global or PingType.Role);
            });
    }

    static async Task<bool> RoleMustExists(
        ulong prop,
        RolesService rolesService,
        ValidationContext<Title> context,
        CancellationToken cancellationToken)
    {
        var rolesResult = await rolesService.GetRoleFromGuildAsync(prop.ToString(), cancellationToken);
        if (rolesResult.IsSuccess)
            return true;

        context.AddFailure(string.Join("; ", rolesResult.Errors.Select(error => error.Message)));
        return false;
    }
}
