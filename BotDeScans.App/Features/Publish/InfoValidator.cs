using FluentValidation;
using static BotDeScans.App.Features.Publish.PublishState;

namespace BotDeScans.App.Features.Publish;

public class InfoValidator : AbstractValidator<Info>
{
    public InfoValidator()
    {
        RuleFor(model => model.ChapterVolume)
            .Must(prop => int.TryParse(prop, out _))
            .When(prop => prop is not null)
            .WithMessage("Volume precisa ser um valor numérico. " +
                         "Caso o capítlo não seja parte de um volume atualmente definido, não preencha o campo.");
    }
}
