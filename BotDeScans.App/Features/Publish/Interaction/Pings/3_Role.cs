using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Discord;
using FluentResults;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Features.Publish.Interaction.Pings;

public class RolePing(
    RolesService rolesService,
    IConfiguration configuration) : Ping(configuration)
{
    protected override PingType Type => PingType.Role;

    public override async Task<Result<string>> GetPingAsTextAsync(State state, CancellationToken cancellationToken)
    {
        var roleResult = await rolesService.GetRoleAsync(state.Title.DiscordRoleId.ToString()!, cancellationToken);
        var text = roleResult.ValueOrDefault?.ToDiscordString();

        return roleResult.Map(text!);
    }
}