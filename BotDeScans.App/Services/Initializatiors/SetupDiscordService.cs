using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.Pings;
using BotDeScans.App.Services.Discord;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Remora.Discord.Commands.Services;
using Remora.Rest.Core;
namespace BotDeScans.App.Services.Initializatiors;

public class SetupDiscordService(
    IConfiguration configuration,
    RolesService rolesService,
    SlashService slashService)
{
    // todo: esses setups podem ser divididos entre validadores e execução
    public async Task<Result> SetupAsync(CancellationToken stoppingToken)
    {
        // todo: podemos pensar em habilitar comando somente do que está configurado e validado
        // ex: mega está no publish? habilitamos! Não está? não habilitamos!
        Console.WriteLine("Updating Discord Slash Commands...");

        var aggregatedResult = Result.Ok();

        // mandatory keys
        var discordToken = configuration.GetValue<string?>("Discord:Token");
        if (string.IsNullOrWhiteSpace(discordToken))
            aggregatedResult = aggregatedResult.WithError("'Discord:Token': value not found in config.json.");

        var releaseChannel = configuration.GetValue<ulong?>("Discord:ReleaseChannel");
        if (releaseChannel is null or default(ulong))
            aggregatedResult = aggregatedResult.WithError("'Discord:ReleaseChannel': value not found in config.json.");

        var serverId = configuration.GetValue<ulong?>("Discord:ServerId");
        if (serverId is null or default(ulong))
            aggregatedResult = aggregatedResult.WithError("'Discord:ServerId': value not found in config.json.");

        else
        {
            // config dependable keys
            var pingTypeAsString = configuration.GetValue<string?>(Ping.PING_TYPE_KEY, null);
            if (Enum.TryParse<PingType>(pingTypeAsString, out var pingType) && pingType == PingType.Global)
            {
                var globalPingValue = configuration.GetValue<string?>(GlobalPing.GLOBAL_ROLE_KEY, null);
                var globalPingErrors = string.IsNullOrWhiteSpace(globalPingValue)
                    ? [new Error($"'{GlobalPing.GLOBAL_ROLE_KEY}': value not found in config.json; mandatory for '{Ping.PING_TYPE_KEY}': '{PingType.Global}'.")]
                    : (await rolesService.GetRoleFromGuildAsync(globalPingValue!, stoppingToken)).Errors;

                aggregatedResult = aggregatedResult.WithErrors(globalPingErrors);
            }

            // update slash commands/autocompletes
            var updateSlashResult = await slashService.UpdateSlashCommandsAsync(new Snowflake(serverId!.Value), ct: stoppingToken);
            if (updateSlashResult.IsSuccess is false)
                aggregatedResult = aggregatedResult.WithError("Failed to update Discord slash commands.");
        }

        return aggregatedResult;
    }
}