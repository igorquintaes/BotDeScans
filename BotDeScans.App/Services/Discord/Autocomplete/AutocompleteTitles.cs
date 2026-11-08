using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;

namespace BotDeScans.App.Services.Discord.Autocomplete;

public class AutocompleteTitles(TitleRepository titleRepository) : IAutocompleteProvider
{
    public const string ID = "autocomplete::titles";

    public string Identity => ID;

    public async ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync(
        IReadOnlyList<IApplicationCommandInteractionDataOption> options,
        string userInput,
        CancellationToken cancellationToken = default)
    {
        var titles = await titleRepository.GetTitlesByNameLikeAsync(
            userInput,
            Consts.DISCORD_CHOICE_MAX_RESULTS,
            cancellationToken);

        return titles
            .Select(title => new ApplicationCommandOptionChoice(
                Name: Truncate(title.Name, Consts.DISCORD_PARAM_MAX_LENGTH),
                Value: title.Id))
            .ToList();
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength
            ? string.Concat(value.AsSpan(0, maxLength - 3), "...")
            : value;
}
