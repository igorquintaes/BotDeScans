using BotDeScans.App.Extensions;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
namespace BotDeScans.App.Services.Discord;

public class RolesService(IConfiguration configuration, IDiscordRestGuildAPI discordRestGuildAPI)
{
    public const string DISCORD_SERVERID_KEY = "Discord:ServerId";

    public virtual async Task<Result<IRole>> GetRoleFromGuildAsync(
        string roleToGet,
        CancellationToken cancellationToken = default)
    {
        var serverId = new Snowflake(configuration.GetRequiredValue<ulong>(DISCORD_SERVERID_KEY));

        // todo: podemos adicionar um cache aqui, de alguns minutos, para diminuir as requests ao discord em um publish.
        var guildRolesResult = await discordRestGuildAPI.GetGuildRolesAsync(serverId, cancellationToken);
        if (!guildRolesResult.IsDefined(out var guildRoles))
            return Result.Fail(guildRolesResult.Error!.Message);

        var role = guildRoles.FirstOrDefault(guildRole =>
            guildRole.ID.Value.ToString().Equals(roleToGet, StringComparison.Ordinal) ||
            guildRole.Name.Equals(roleToGet, StringComparison.Ordinal));

        return role is null
            ? Result.Fail($"Cargo não encontrado no servidor: {roleToGet}")
            : Result.Ok(role);
    }
}
