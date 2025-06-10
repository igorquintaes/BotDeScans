using BotDeScans.App.Attributes;
using BotDeScans.App.Builders;
using BotDeScans.App.Services.Discord.Autocomplete;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.ComponentModel;
namespace BotDeScans.App.Features.Titles.Update;

[Group("title")]
public class Commands(
    IOperationContext context,
    IDiscordRestInteractionAPI interactionAPI,
    IFeedbackService feedbackService,
    Persistence persistence) : CommandGroup
{
    [Command("update")]
    [RoleAuthorize("Publisher")]
    [SuppressInteractionResponse(true)]
    [Description("Atualiza dados da obra")]
    public async Task<IResult> ExecuteAsync(
        [AutocompleteProvider(AutocompleteTitles.Id)]
        [Description("Nome da obra")]
        int title)
    {
        if (context is not InteractionContext interactionContext)
            return Result.FromSuccess();

        var dbTitle = await persistence.GetTitleAsync(title, CancellationToken);
        if (dbTitle is null)
            return await feedbackService.SendContextualWarningAsync(
                "Obra não encontrada.",
                ct: CancellationToken);

        var modal = new ModalBuilder(Interactions.MODAL_NAME, "Atualizar Obra")
            .AddField(fieldName: "name", value: dbTitle.Name, label: "Nome da obra")
            .AddField(fieldName: "role", value: dbTitle.DiscordRoleId.ToString(), label: "Cargo do Discord (Nome ou ID)", isRequired: false)
            .CreateWithState(dbTitle.Id.ToString());

        return await interactionAPI.CreateInteractionResponseAsync(
            interactionContext.Interaction.ID,
            interactionContext.Interaction.Token,
            new InteractionResponse(InteractionCallbackType.Modal, modal),
            ct: CancellationToken);
    }
}
