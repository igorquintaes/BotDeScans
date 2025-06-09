using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Discord.Cache;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
namespace BotDeScans.App.Services.Discord;

public class RolesService(
    IConfiguration configuration, 
    ScopedRoleCache scopedRoleCache,
    IDiscordRestGuildAPI discordRestGuildAPI)
{
    public const string DISCORD_SERVERID_KEY = "Discord:ServerId";

    public virtual async Task<Result<IRole>> GetRoleFromGuildAsync(
        string roleToGet,
        CancellationToken cancellationToken = default)
    {
        if (scopedRoleCache.NeedsCache)
        {
            var serverId = new Snowflake(configuration.GetRequiredValue<ulong>(DISCORD_SERVERID_KEY));

            var guildRolesResult = await discordRestGuildAPI.GetGuildRolesAsync(serverId, cancellationToken);
            if (!guildRolesResult.IsDefined(out var guildRoles))
                return Result.Fail(guildRolesResult.Error!.Message);

            scopedRoleCache.Roles = guildRoles;
        }

        var role = scopedRoleCache.Roles.FirstOrDefault(guildRole =>
            guildRole.ID.Value.ToString().Equals(roleToGet, StringComparison.Ordinal) ||
            guildRole.Name.Equals(roleToGet, StringComparison.Ordinal));

        return role is not null
            ? Result.Ok(role)
            : Result.Fail($"Cargo não encontrado no servidor: {roleToGet}");
    }
}
