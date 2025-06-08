using BotDeScans.App.Attributes;
using BotDeScans.App.Builders;
using BotDeScans.App.Infra;
using BotDeScans.App.Services.Discord.Autocomplete;
using Microsoft.EntityFrameworkCore;
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
    FeedbackService feedbackService,
    DatabaseContext databaseContext) : CommandGroup
{
    [Command("update")]
    [RoleAuthorize("Publisher")]
    [SuppressInteractionResponse(true)]
    [Description("Atualiza dados da obra")]
    public async Task<IResult> Update(
        [AutocompleteProvider(AutocompleteTitles.Id)]
        [Description("Nome da obra")]
        int titleId)
    {
        if (context is not InteractionContext interactionContext)
            return Result.FromSuccess();

        var title = await databaseContext.Titles.FirstOrDefaultAsync(x => x.Id == titleId);
        if (title is null)
            return await feedbackService.SendContextualWarningAsync(
                "Obra não encontrada.",
                ct: CancellationToken);

        var modal = new ModalBuilder("UpdateAsync", "Atualizar Obra")
            .AddField(fieldName: "name", value: title.Name, label: "Nome da obra")
            .AddField(fieldName: "role", value: title.DiscordRoleId.ToString(), label: "Cargo do Discord (Nome ou ID)", isRequired: false)
            .CreateWithState(title.Id.ToString());

        var response = new InteractionResponse(InteractionCallbackType.Modal, modal);
        return await interactionAPI.CreateInteractionResponseAsync
        (
            interactionContext.Interaction.ID,
            interactionContext.Interaction.Token,
            response,
            ct: CancellationToken
        );
    }
}
