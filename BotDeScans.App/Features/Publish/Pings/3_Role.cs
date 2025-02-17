using BotDeScans.App.Extensions;
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
        // todo: mover para passo de validação
        if (publishState.Title.DiscordRoleId is null)
            return Result.Fail("Não foi definida uma role para o Discord nesta obra. Defina, ou mude o tipo de publicação no arquivo de configuração do Bot de Scans.");

        var roleResult = await rolesService.GetRoleFromGuildAsync(publishState.Title.DiscordRoleId.ToString()!, cancellationToken);
        if (roleResult.IsFailed)
            return roleResult.ToResult();

        return roleResult.Value.ToDiscordString();
    }
}