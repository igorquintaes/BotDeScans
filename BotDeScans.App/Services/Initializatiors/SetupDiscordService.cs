using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Titles;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Remora.Discord.Commands.Services;
using Remora.Rest.Core;
namespace BotDeScans.App.Services.Initializatiors;

public class SetupDiscordService(
    IConfiguration configuration,
    SlashService slashService)
{
    public async Task<Result> SetupAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Updating Discord Slash Commands...");

        // mandatory keys
        _ = configuration.GetRequiredValue<string>("Discord:Token");
        _ = configuration.GetRequiredValue<ulong>("Discord:ReleaseChannel");
        var serverId = configuration.GetRequiredValue<ulong>("Discord:ServerId");

        // update slash with new commands and autocomplete new values
        var updateSlash = await slashService.UpdateSlashCommandsAsync(new Snowflake(serverId), ct: stoppingToken);
        if (!updateSlash.IsSuccess)
            return Result.Fail("Failed to update Discord slash commands");

        return Result.Ok();
    }
}