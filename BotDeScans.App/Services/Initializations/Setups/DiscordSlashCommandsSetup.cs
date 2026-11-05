using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Wrappers;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Remora.Rest.Core;

namespace BotDeScans.App.Services.Initializations.Setups;

public class DiscordSlashCommandsSetup(
    IConfiguration configuration,
    SlashServiceWrapper slashService) : ISetup
{
    public virtual async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Updating Discord Slash Commands...");

        var serverIdResult = configuration.GetRequiredValue<ulong>("Discord:ServerId");
        var updateSlashResult = await slashService.UpdateSlashCommandsAsync(new Snowflake(serverIdResult), ct: cancellationToken);

        var result = updateSlashResult.ToFluentResult();
        if (result.IsFailed)
            return result;

        return result.WithSuccess("Discord slash commands setup completed.");

    }
}