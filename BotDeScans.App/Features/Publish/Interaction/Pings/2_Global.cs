using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Discord;
using FluentResults;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Features.Publish.Interaction.Pings;

public class GlobalPing(
    RolesService rolesService,
    IConfiguration configuration) : Ping(configuration)
{
    public const string GLOBAL_ROLE_KEY = "Settings:Publish:GlobalRole";

    protected override PingType Type => PingType.Global;

    public override async Task<Result<string>> GetPingAsTextAsync(State state,CancellationToken cancellationToken)
    {
        var globalRoleName = configuration.GetRequiredValue<string>(GLOBAL_ROLE_KEY);
        var globalRoleAsPingResult = await rolesService.GetRoleAsync(globalRoleName, cancellationToken);
        var titleRoleAsPingResult = await rolesService.GetRoleAsync(state.Title.DiscordRoleId.ToString()!, cancellationToken);

        if (globalRoleAsPingResult.IsFailed || titleRoleAsPingResult.IsFailed)
            return new Result()
                  .WithReasons(globalRoleAsPingResult.Reasons)
                  .WithReasons(titleRoleAsPingResult.Reasons);

        var pingText = $"{globalRoleAsPingResult.Value.ToDiscordString()}, " +
                       $"{titleRoleAsPingResult.Value.ToDiscordString()}";

        return Result.Ok(pingText)
                     .WithReasons(globalRoleAsPingResult.Reasons)
                     .WithReasons(titleRoleAsPingResult.Reasons);
    }
}
