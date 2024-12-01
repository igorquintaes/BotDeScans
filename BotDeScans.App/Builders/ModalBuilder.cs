using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Interactivity;

namespace BotDeScans.App.Builders;

public class ModalBuilder(string ModalId, string Title) : List<ActionRowComponent>
{
    public ModalBuilder AddField(
        string fieldName,
        string label,
        bool isRequired = true,
        TextInputStyle style = TextInputStyle.Short,
        string? value = null)
    {
        if (Count == 5)
            throw new ArgumentOutOfRangeException(nameof(fieldName), "Cannot build a modal with more than 5 fields (Discord API validation).");

        Add(new ActionRowComponent([
            new TextInputComponent(
                CustomID: fieldName,
                Style: style,
                Label: label,
                MinLength: default,
                MaxLength: default,
                IsRequired: isRequired,
                Value: value ?? string.Empty,
                Placeholder: default
            )]));

        return this;
    }

    public OneOf<IInteractionMessageCallbackData, IInteractionAutocompleteCallbackData, IInteractionModalCallbackData> Create()
        => new InteractionModalCallbackData(CustomIDHelpers.CreateModalID(ModalId), Title, this);

    public OneOf<IInteractionMessageCallbackData, IInteractionAutocompleteCallbackData, IInteractionModalCallbackData> CreateWithState(string state)
        => new InteractionModalCallbackData(CustomIDHelpers.CreateModalIDWithState(ModalId, state), Title, this);
}