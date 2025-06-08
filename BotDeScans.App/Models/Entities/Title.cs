using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Pings;
using BotDeScans.App.Services.Discord;
using FluentValidation;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Models.Entities;

public class Title
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public required ulong? DiscordRoleId { get; set; }
    public List<TitleReference> References { get; init; } = [];

    public void AddOrUpdateReference(ExternalReference key, string value)
    {
        var reference = References.SingleOrDefault(r => r.Key == key);

        if (reference is null)
            References.Add(new TitleReference { Key = key, Value = value });
        else
            reference.Value = value;
    }
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
                    .MustAsync(async (_, prop, context, ct) => await RoleMustExists(prop!.Value, rolesService, context, ct))
                    .When(prop => prop.DiscordRoleId.HasValue &&
                                  prop.DiscordRoleId != default(ulong) &&
                                  pingType is PingType.Global or PingType.Role);
            });
    }

    private static async Task<bool> RoleMustExists(
        ulong prop,
        RolesService rolesService,
        ValidationContext<Title> context,
        CancellationToken cancellationToken)
    {
        var rolesResult = await rolesService.GetRoleFromGuildAsync(prop.ToString(), cancellationToken);
        if (rolesResult.IsSuccess)
            return true;

        context.AddFailure(rolesResult.ToValidationErrorMessage());
        return true;
    }
}
