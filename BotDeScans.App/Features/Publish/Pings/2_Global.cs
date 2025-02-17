using BotDeScans.App.Extensions;
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
        // todo: mover para passo de validação
        if (publishState.Title.DiscordRoleId is null)
            return Result.Fail("Não foi definida uma role para o Discord nesta obra. Defina, ou mude o tipo de publicação no arquivo de configuração do Bot de Scans.");

        // todo: adicionar validação para chaves de role no validator de publicação
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
