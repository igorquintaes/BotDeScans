using BotDeScans.App.Features.Publish.Pings;
using BotDeScans.App.Models;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using static BotDeScans.App.Features.Publish.PublishState;

namespace BotDeScans.App.Features.Publish;

public class PublishStateValidator : AbstractValidator<PublishState>
{
    public PublishStateValidator(
        IValidator<Title> titleValidator, 
        IValidator<Info> infoValidator)
    {
        RuleFor(model => model.Title).SetValidator(titleValidator);
        RuleFor(model => model.ReleaseInfo).SetValidator(infoValidator);
    }
}

public class TitleValidator : AbstractValidator<Title>
{
    public TitleValidator(IConfiguration configuration)
    {
        var pingTypeAsString = configuration.GetValue<string?>(Ping.PING_TYPE_KEY, null);
        var globalPingValue = configuration.GetValue<string?>(GlobalPing.GLOBAL_ROLE_KEY, null);
        var isPingTypeValid = Enum.TryParse<PingType>(pingTypeAsString, out var pingType);

        RuleFor(model => model)
            .Must(_ => string.IsNullOrWhiteSpace(pingTypeAsString) is false)
            .WithMessage("É necessário definir um tipo de ping no arquivo de configuração do Bot de Scans.");

        RuleFor(model => model)
            .Must(_ => isPingTypeValid)
            .When(_ => pingTypeAsString is not null)
            .WithMessage("Valor inválido para o tipo de ping no arquivo de configuração do Bot de Scans.");

        RuleFor(model => model)
            .Must(_ => string.IsNullOrWhiteSpace(globalPingValue) is false)
            .When(_ => pingType == PingType.Global)
            .WithMessage("É necessário definir um valor para ping global no arquivo de configuração do Bot de Scans.");

        RuleFor(model => model.DiscordRoleId)
            .Must(prop => prop.HasValue && prop.Value != default)
            .When(prop => pingType == PingType.Global || pingType == PingType.Role)
            .WithMessage($"Não foi definida uma role para o Discord nesta obra, obrigatória para o ping de tipo {pingType}. " +
                         $"Defina, ou mude o tipo de ping para publicação no arquivo de configuração do Bot de Scans.");
    }
}

public partial class InfoValidator : AbstractValidator<Info>
{
    public InfoValidator()
    {
        RuleFor(model => model.ChapterName)
            .Must(prop => prop!.Length <= 255)
            .When(prop => string.IsNullOrWhiteSpace(prop.ChapterName) is false)
            .WithMessage("Nome de capítulo muito longo.");

        RuleFor(model => model.ChapterNumber)
            .Must(prop => ChapterNumberPattern().Match(prop).Success)
            .WithMessage("Número do capítulo inválido.");

        RuleFor(model => model.ChapterVolume)
            .Must(prop => int.TryParse(prop, out var volume) && volume >= 0)
            .When(prop => string.IsNullOrWhiteSpace(prop.ChapterVolume) is false)
            .WithMessage("Volume do capítulo inválido.");
    }

    [GeneratedRegex("^(0|[1-9]\\d*)((.\\d+){1,2})?[a-z]?$")]
    private static partial Regex ChapterNumberPattern();
}
