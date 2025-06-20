﻿using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Discord;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Features.Publish.Interaction.Pings;

public class RolePing(
    State publishState,
    RolesService rolesService,
    IConfiguration configuration) : Ping(configuration)
{
    protected override PingType Type => PingType.Role;

    public override async Task<string> GetPingAsTextAsync(CancellationToken cancellationToken)
    {
        var roleResult = await rolesService.GetRoleAsync(publishState.Title.DiscordRoleId.ToString()!, cancellationToken);

        return roleResult.Value.ToDiscordString();
    }
}