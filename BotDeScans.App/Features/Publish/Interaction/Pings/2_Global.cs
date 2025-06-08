using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Discord;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Features.Publish.Interaction.Pings;

public class GlobalPing(
    State state,
    RolesService rolesService,
    IConfiguration configuration) : Ping(configuration)
{
    public const string GLOBAL_ROLE_KEY = "Settings:Publish:GlobalRole";

    protected override PingType Type => PingType.Global;

    public override async Task<string> GetPingAsTextAsync(CancellationToken cancellationToken)
    {
        var globalRoleName = configuration.GetRequiredValue<string>(GLOBAL_ROLE_KEY);
        var globalRoleAsPingResult = await rolesService.GetRoleFromGuildAsync(globalRoleName, cancellationToken);
        var titleRoleAsPingResult = await rolesService.GetRoleFromGuildAsync(state.Title.DiscordRoleId.ToString()!, cancellationToken);

        return $"{globalRoleAsPingResult.Value.ToDiscordString()}, {titleRoleAsPingResult.Value.ToDiscordString()}";
    }
}
