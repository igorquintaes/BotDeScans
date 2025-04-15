using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Services.Discord;
using FluentResults;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Features.Publish.Pings;

public class GlobalPing(
    PublishState publishState,
    RolesService rolesService,
    IConfiguration configuration) : Ping(configuration)
{
    public const string GLOBAL_ROLE_KEY = "Settings:Publish:GlobalRole";

    protected override PingType Type => PingType.Global;

    public override async Task<Result<string>> GetPingAsTextAsync(CancellationToken cancellationToken)
    {
        var globalRoleName = configuration.GetRequiredValue<string>(GLOBAL_ROLE_KEY);
        var globalRoleAsPingResult = await rolesService.GetRoleFromGuildAsync(globalRoleName, cancellationToken);
        if (globalRoleAsPingResult.IsFailed)
            return globalRoleAsPingResult.ToResult();

        var titleRoleAsPingResult = await rolesService.GetRoleFromGuildAsync(publishState.Title.DiscordRoleId.ToString()!, cancellationToken);
        if (titleRoleAsPingResult.IsFailed)
            return titleRoleAsPingResult.ToResult();

        return $"{globalRoleAsPingResult.Value.ToDiscordString()}, {titleRoleAsPingResult.Value.ToDiscordString()}";
    }
}
