using FluentValidation;
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

public class FileListValidator : AbstractValidator<Title>
{
    public FileListValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

