using BotDeScans.App.Extensions;
using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services.Discord;
using FluentResults;
using FluentValidation;
using Remora.Discord.API.Abstractions.Objects;

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
        var roleResult = await GetDiscordRole(role, cancellationToken);
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

    private async Task<Result<IRole?>> GetDiscordRole(
        string roleRequest, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(roleRequest))
            return Result.Ok<IRole?>(null);

        var roleResult = await rolesService.GetRoleFromGuildAsync(roleRequest, cancellationToken);

        return roleResult.IsSuccess 
            ? Result.Ok<IRole?>(roleResult.Value) 
            : roleResult.ToResult();
    }
}
