using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.Pings;
using BotDeScans.App.Services.Discord;
using FluentValidation;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Models;

public record Title(string Name, ulong? DiscordRoleId)
{
    public int Id { get; init; }
    public List<TitleReference> References { get; init; } = [];

    public Title UpdateReference(TitleReference titleReference)
    {
        var oldReference = References.FirstOrDefault(x => x.Key != titleReference.Key);
        if (oldReference is null)
            return this with { References = [.. References, titleReference] };

        return this with
        {
            References =
            [
                ..References.Where(x => x.Key != titleReference.Key),
                oldReference with { Value = titleReference.Value }
            ]
        };
    }
}

public class TitleValidator : AbstractValidator<Title>
{
    public TitleValidator(
        RolesService rolesService,
        IConfiguration configuration)
    {
        var pingTypeAsString = configuration.GetValue<string?>(Ping.PING_TYPE_KEY, null);
        var isPingTypeValid = Enum.TryParse<PingType>(pingTypeAsString, out var pingType);

        RuleFor(model => model)
            .Must(_ => string.IsNullOrWhiteSpace(pingTypeAsString) is false)
            .WithMessage("É necessário definir um tipo de ping no arquivo de configuração do Bot de Scans.");

        RuleFor(model => model)
            .Must(_ => isPingTypeValid)
            .When(_ => pingTypeAsString is not null)
            .WithMessage("Valor inválido para o tipo de ping no arquivo de configuração do Bot de Scans.");

        RuleFor(model => model.DiscordRoleId)
            .Cascade(CascadeMode.Stop)
            .Must(prop => prop.HasValue && prop.Value != default)
            .When(prop => pingType is PingType.Global or PingType.Role)
            .WithMessage($"Não foi definida uma role para o Discord nesta obra, obrigatória para o ping de tipo {pingType}. " +
                         $"Defina, ou mude o tipo de ping para publicação no arquivo de configuração do Bot de Scans.")
            .MustAsync(async (_, prop, context, cancellationToken) =>
            {
                var rolesResult = await rolesService.GetRoleFromGuildAsync(prop!.Value.ToString(), cancellationToken);
                if (rolesResult.IsSuccess)
                    return true;

                context.AddFailure(string.Join("; ", rolesResult.Errors.Select(error => error.Message)));
                return false;
            })
            .When(prop => prop.DiscordRoleId.HasValue &&
                          prop.DiscordRoleId.Value != default &&
                          pingType is PingType.Global or PingType.Role);
    }
}
