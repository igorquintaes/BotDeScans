using BotDeScans.App.Extensions;
using BotDeScans.App.Models.Entities.Enums;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;

namespace BotDeScans.App.Services.Discord.Autocomplete;

public class AutocompleteStepNames() : IAutocompleteProvider
{
    public const string ID = "autocomplete::stepnames";
    public const int DISCORD_MAX_RESULTS = 25;
    public const int LAST_STEPS_MANAGEMENT_VALUE_IN_ENUM = 5;

    public string Identity => ID;

    public ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync(
        IReadOnlyList<IApplicationCommandInteractionDataOption> options,
        string userInput,
        CancellationToken ct = default)
    {
        var stepNames = Enum.GetValues<StepName>()
            .Where(x => (int)x > LAST_STEPS_MANAGEMENT_VALUE_IN_ENUM)
            .Select(x => (Name: x, Description: x.GetDescription()))
            .Take(DISCORD_MAX_RESULTS)
            .ToArray();

        var result = stepNames
            .Where(stepName => stepName.Description.Contains(userInput, StringComparison.InvariantCultureIgnoreCase))
            .Select(stepName => new ApplicationCommandOptionChoice(stepName.Description, stepName.Name.ToString()))
            .ToList();

        return ValueTask.FromResult<IReadOnlyList<IApplicationCommandOptionChoice>>(result);
    }
}
