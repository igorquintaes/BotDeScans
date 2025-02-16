using FluentValidation;
using System.Text.RegularExpressions;
using static BotDeScans.App.Features.Publish.PublishState;

namespace BotDeScans.App.Features.Publish;

public partial class InfoValidator : AbstractValidator<Info>
{
    public InfoValidator()
    {
        RuleFor(model => model.ChapterNumber)
            .Must(prop => ChapterNumberPattern().Match(prop).Success)
            .WithMessage("Nome de capítulo inválido.");

        RuleFor(model => model.ChapterName)
            .Must(prop => prop!.Length <= 255)
            .When(prop => prop.ChapterName is not null)
            .WithMessage("Nome de capítulo muito longo.");

        RuleFor(model => model.ChapterVolume)
            .Must(prop => int.TryParse(prop, out _))
            .When(prop => prop.ChapterVolume is not null)
            .WithMessage("Volume precisa ser um valor numérico. " +
                         "Caso o capítulo não seja parte de um volume atualmente definido, não preencha o campo.");
    }

    [GeneratedRegex("^(0|[1-9]\\d*)((.\\d+){1,2})?[a-z]?$")]
    private static partial Regex ChapterNumberPattern();
}
