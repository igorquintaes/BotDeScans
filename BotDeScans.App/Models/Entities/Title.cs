using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Pings;
using BotDeScans.App.Models.Entities.Enums;
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
    public List<SkipStep> SkipSteps { get; init; } = [];

    public void AddOrUpdateReference(ExternalReference key, string value)
    {
        var reference = References.SingleOrDefault(r => r.Key == key);

        if (reference is null)
            References.Add(new TitleReference { Key = key, Value = value });
        else
            reference.Value = value;
    }

    public void AddSkipStep(StepName step)
    {
        if (SkipSteps.Any(s => s.Step == step))
            return;

        SkipSteps.Add(new SkipStep { Step = step });
    }

    public void RemoveSkipStep(StepName step)
    {
        var stepToRemove = SkipSteps.SingleOrDefault(s => s.Step == step);
        if (stepToRemove is not null)
            SkipSteps.Remove(stepToRemove);
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
            .MustAsync(async (_, prop, context, ct) =>
            {
                if (prop.HasValue is false || prop == default(ulong))
                    context.AddFailure($"Não foi definida uma role para o Discord nesta obra, obrigatória para o ping de tipo {pingType}. " +
                                        "Defina, ou mude o tipo de ping para publicação no arquivo de configuração do Bot de Scans.");
                else
                {
                    var rolesResult = await rolesService.GetRoleAsync(prop.ToString()!, ct);
                    if (rolesResult.IsFailed)
                        context.AddFailure(rolesResult.ToValidationErrorMessage());
                }

                return true;
            })
            .When(prop => pingType is PingType.Global or PingType.Role);
    }
}
