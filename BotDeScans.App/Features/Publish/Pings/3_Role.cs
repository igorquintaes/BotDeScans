using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.State;
using BotDeScans.App.Services.Discord;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Features.Publish.Pings;

public class RolePing(
    PublishState publishState,
    RolesService rolesService,
    IConfiguration configuration) : Ping(configuration)
{
    protected override PingType Type => PingType.Role;

    public override async Task<string> GetPingAsTextAsync(CancellationToken cancellationToken)
    {
        var roleResult = await rolesService.GetRoleFromGuildAsync(publishState.Title.DiscordRoleId.ToString()!, cancellationToken);

        return roleResult.Value.ToDiscordString();
    }
}