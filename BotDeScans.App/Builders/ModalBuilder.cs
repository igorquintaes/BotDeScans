using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Interactivity;
using Remora.Rest.Core;

namespace BotDeScans.App.Builders;

public class ModalBuilder(string ModalId, string Title)
{
    private List<ActionRowComponent> fields = [];

    public ModalBuilder AddField(
        string fieldName,
        string label,
        bool isRequired = true,
        TextInputStyle style = TextInputStyle.Short)
    {
        if (fields.Count == 5)
            throw new ArgumentOutOfRangeException(nameof(fieldName), "Can't build a modal with more than 5 fields (Discord API validation).");

        fields.Add(new ActionRowComponent(new[] {
            new TextInputComponent(
                CustomID: fieldName,
                Style: style,
                Label: label,
                MinLength: default,
                MaxLength: default,
                IsRequired: isRequired,
                Value: string.Empty,
                Placeholder: default
            )}));

        return this;
    }

    public Optional<OneOf<
        IInteractionMessageCallbackData,
        IInteractionAutocompleteCallbackData,
        IInteractionModalCallbackData>> Create() 
        => new(new InteractionModalCallbackData(CustomIDHelpers.CreateModalID(ModalId), Title, fields));
}