using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;

namespace BotDeScans.App.Services.Discord.Autocomplete;

internal class AutocompleteStepNames() : IAutocompleteProvider
{
    public const string ID = "autocomplete::stepnames";
    public const int DISCORD_MAX_RESULTS = 25;

    public string Identity => ID;

    public ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync(
        IReadOnlyList<IApplicationCommandInteractionDataOption> options,
        string userInput,
        CancellationToken ct = default)
    {
        var stepNames = Enum.GetValues<StepName>()
            .Where(x => (int)x > 5)
            .Select(x => (Name: x, Description: x.GetDescription()))
            .Take(DISCORD_MAX_RESULTS)
            .ToArray();

        var result = stepNames
            .Where(stepName => stepName.Description.Contains(userInput))
            .Select(stepName => new ApplicationCommandOptionChoice(stepName.Description, stepName.Name.ToString()))
            .ToList();

        return ValueTask.FromResult<IReadOnlyList<IApplicationCommandOptionChoice>>(result);
    }
}
