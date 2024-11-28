using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Discord;
using FluentValidation;

namespace BotDeScans.App.Features.Titles.Create;

public record CreateRequest(string Name, string? Role);

public class FileListValidator : AbstractValidator<CreateRequest>
{
    public FileListValidator(RolesService rolesService)
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Role).CustomAsync(
            async (roleName, context, cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(roleName))
                    return;

                var role = await rolesService.GetRoleFromDiscord(roleName!, cancellationToken);
                if (role.IsFailed)
                    context.AddFailure(nameof(CreateRequest.Role), role.Errors);
            });
    }
}