using BotDeScans.App.Attributes;
using BotDeScans.App.Builders;
using BotDeScans.App.Features.Titles;
using BotDeScans.App.Infra;
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
namespace BotDeScans.App.Features.Publish.Discord;

public class PublishCommands(
    DatabaseContext databaseContext,
    IOperationContext context,
    IFeedbackService feedbackService,
    IDiscordRestInteractionAPI interactionAPI) : CommandGroup
{
    [Command("publish")]
    [RoleAuthorize("Publisher")]
    [SuppressInteractionResponse(true)]
    [Description("Abre uma modal com as opções de publicação de um novo lançamento")]
    public async Task<IResult> Publish(
        [AutocompleteProvider(AutocompleteTitles.Id)]
        [Description("Nome da obra")]
        string title)
    {
        if (context is not InteractionContext interactionContext)
            return Result.FromSuccess();

        var titleId = await databaseContext.Titles.Where(x => x.Name == title).Select(x => x.Id).SingleOrDefaultAsync();
        if (titleId == default)
            return await feedbackService.SendContextualWarningAsync(
                "Obra não encontrada.",
                ct: CancellationToken);

        var modal = new ModalBuilder(nameof(PublishInteractions.PublishAsync), "Publicar novo lançamento")
            .AddField(fieldName: "driveUrl", label: "Link do capítulo")
            .AddField(fieldName: "chapterName", label: "Nome do capítulo")
            .AddField(fieldName: "chapterNumber", label: "Número do capítulo")
            .AddField(fieldName: "chapterVolume", label: "Número do Volume", isRequired: false)
            .AddField(fieldName: "message", label: "Mensagem de postagem", isRequired: false, TextInputStyle.Paragraph)
            .CreateWithState(titleId.ToString());

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
