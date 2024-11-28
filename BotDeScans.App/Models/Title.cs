using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Discord;
using FluentValidation;
namespace BotDeScans.App.Models;

public record Title(string Name, string? DiscordRole)
{
    public int Id { get; init; }
    public List<TitleReference> References { get; init; } = [];
}

public class FileListValidator : AbstractValidator<Title>
{
    public FileListValidator(RolesService rolesService)
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.DiscordRole).CustomAsync(
            async (roleName, context, cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(roleName))
                    return;

                var role = await rolesService.GetRoleFromDiscord(roleName!, cancellationToken);
                if (role.IsFailed)
                    context.AddFailure(nameof(Title.DiscordRole), role.Errors);
            });
    }
}

