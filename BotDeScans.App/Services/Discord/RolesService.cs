using BotDeScans.App.Extensions;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
namespace BotDeScans.App.Services.Discord;

public class RolesService(IConfiguration configuration, IDiscordRestGuildAPI discordRestGuildAPI)
{
    public virtual Result<bool> ContainsAtLeastOneOfExpectedRoles(
        IEnumerable<string> expectedRoles,
        IEnumerable<IRole> guildRoles,
        IEnumerable<Snowflake> userRoles)
    {
        var serverExpectedRoles = guildRoles
            .Where(guildRole => expectedRoles
            .Contains(guildRole.Name));

        if (!serverExpectedRoles.Any())
            return Result.Fail($"Invalid request. No role(s) found in server; {string.Join(", ", expectedRoles)}");

        var hasRequiredRole = userRoles
            .Any(id => serverExpectedRoles
            .Any(requiredRole => requiredRole.ID == id));

        return Result.Ok(hasRequiredRole);
    }

    public virtual async Task<Result<IRole>> GetRoleFromDiscord(
        string roleName, 
        CancellationToken cancellationToken)
    {
        var serverId = configuration.GetRequiredValue<ulong>("Discord:ServerId");
        var guildRolesResult = await discordRestGuildAPI.GetGuildRolesAsync(new Snowflake(serverId), cancellationToken);

        if (!guildRolesResult.IsDefined(out var guildRoles))
            return Result.Fail(guildRolesResult.Error!.Message);

        var guildRole = guildRoles.FirstOrDefault(guildRole => roleName.Equals(guildRole.Name, StringComparison.Ordinal));
        return guildRole is not null
            ? Result.Ok(guildRole)
            : Result.Fail("Cargo não encontrado no servidor.");
    }
}
