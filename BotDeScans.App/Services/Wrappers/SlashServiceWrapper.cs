using Remora.Discord.Commands.Services;
using Remora.Rest.Core;
using Remora.Results;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Services.Wrappers;

[ExcludeFromCodeCoverage(Justification = $"Be able to mock and write tests for {nameof(SlashService)}")]
public class SlashServiceWrapper(SlashService slashService)
{
    public virtual async Task<IResult> UpdateSlashCommandsAsync(
        Snowflake? guildID = null,
        string? treeName = null,
        CancellationToken ct = default) =>
        await slashService.UpdateSlashCommandsAsync(guildID, treeName, ct);
}
