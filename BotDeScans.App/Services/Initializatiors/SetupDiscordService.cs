using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Remora.Discord.Commands.Services;
using Remora.Rest.Core;
namespace BotDeScans.App.Services.Initializatiors;

internal class SetupDiscordService(IConfiguration configuration, SlashService slashService, LoggerService loggerService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Updating Discord Slash Commands - Started");
        _ = configuration.GetRequiredValue<string>("Discord:Token");
        _ = configuration.GetRequiredValue<ulong>("Discord:ReleaseChannel");
        var serverId = configuration.GetRequiredValue<ulong>("Discord:ServerId");

        var updateSlash = await slashService.UpdateSlashCommandsAsync(new Snowflake(serverId), ct: stoppingToken);
        if (!updateSlash.IsSuccess)
            loggerService.LogErrors("Failed to update Discord slash commands", updateSlash);

        Console.WriteLine("Updating Discord Slash Commands - Done");
    }
}