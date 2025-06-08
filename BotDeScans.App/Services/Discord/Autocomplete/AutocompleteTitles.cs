using BotDeScans.App.Infra;
using BotDeScans.App.Models;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;
namespace BotDeScans.App.Services.Discord.Autocomplete;

public class AutocompleteTitles(DatabaseContext databaseContext) : IAutocompleteProvider
{
    public const string Id = "autocomplete::titles";
    public string Identity => Id;

    public async ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync(
        IReadOnlyList<IApplicationCommandInteractionDataOption> options,
        string userInput,
        CancellationToken ct = default)
    {
        var titles = await databaseContext.Titles
            .Where(x => EF.Functions.Like(x.Name, $"%{userInput}%"))
            .Take(25)
            .ToArrayAsync(ct);

        return titles
            .Select(title => new ApplicationCommandOptionChoice(
                Name: title.Name.Length > Consts.DISCORD_PARAM_MAX_LENGTH
                    ? string.Concat(title.Name.AsSpan(0, 96), "...")
                    : title.Name,
                Value: title.Id))
            .ToList();
    }
}
