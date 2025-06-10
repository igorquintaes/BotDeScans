using BotDeScans.App.Extensions;
using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services.Discord;
using FluentResults;
using FluentValidation;

namespace BotDeScans.App.Features.Titles.Create;

public class Handler(
    DatabaseContext databaseContext,
    RolesService rolesService,
    IValidator<Title> validator)
{
    public virtual async Task<Result> ExecuteAsync(
        string name,
        string role,
        CancellationToken cancellationToken)
    {
        var roleResult = await rolesService.GetOptionalRoleAsync(role, cancellationToken);
        if (roleResult.IsFailed)
            return roleResult.ToResult();

        var title = new Title { Name = name, DiscordRoleId = roleResult.Value?.ID.Value };
        var validationResult = await validator.ValidateAsync(title, cancellationToken);
        if (validationResult.IsValid is false)
            return validationResult.ToResult();

        await databaseContext.AddAsync(title, cancellationToken);
        await databaseContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
