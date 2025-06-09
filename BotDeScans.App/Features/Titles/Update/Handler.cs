using BotDeScans.App.Extensions;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services.Discord;
using FluentResults;
using FluentValidation;

namespace BotDeScans.App.Features.Titles.Update;

public class Handler(
    Persistence persistence,
    RolesService rolesService,
    IValidator<Title> validator)
{
    public virtual async Task<Result> ExecuteAsync(
        string name,
        string? role,
        int titleId,
        CancellationToken cancellationToken)
    {
        var title = await persistence.GetTitleAsync(titleId, cancellationToken);
        if (title is null)
            return Result.Fail("Obra não encontrada.");

        var roleResult = await rolesService.GetOptionalRoleAsync(role, cancellationToken);
        if (roleResult.IsFailed)
            return roleResult.ToResult();

        title.Name = name;
        title.DiscordRoleId = roleResult.Value?.ID.Value;

        var validatioResult = await validator.ValidateAsync(title, cancellationToken);
        if (validatioResult.IsValid is false)
            return validatioResult.ToResult();

        await persistence.SaveAsync(cancellationToken);

        return Result.Ok();
    }
}
