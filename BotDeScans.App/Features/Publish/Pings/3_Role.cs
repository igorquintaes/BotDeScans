using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Services.Discord;
using FluentResults;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Features.Publish.Pings;

public class RolePing(
    PublishState publishState,
    RolesService rolesService,
    IConfiguration configuration) : Ping(configuration)
{
    protected override PingType Type => PingType.Role;

    public override async Task<Result<string>> GetPingAsTextAsync(CancellationToken cancellationToken)
    {
        var roleResult = await rolesService.GetRoleFromGuildAsync(publishState.Title.DiscordRoleId.ToString()!, cancellationToken);
        if (roleResult.IsFailed)
            return roleResult.ToResult();

        return roleResult.Value.ToDiscordString();
    }
}