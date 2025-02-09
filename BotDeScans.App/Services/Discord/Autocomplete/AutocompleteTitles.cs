using BotDeScans.App.Infra;
using FuzzySharp;
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
        var titles = await databaseContext.Titles.Select(x => x.Name).ToArrayAsync(ct);

        return titles
            .OrderByDescending(title => Fuzz.Ratio(userInput, title))
            .Take(25)
            .Select(title => new ApplicationCommandOptionChoice(title, title))
            .ToList();
    }
}
