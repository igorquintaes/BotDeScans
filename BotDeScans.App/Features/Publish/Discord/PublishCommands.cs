using BotDeScans.App.Attributes;
using BotDeScans.App.Builders;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System.ComponentModel;
namespace BotDeScans.App.Features.Publish.Discord;

public class PublishCommands(
    IOperationContext context,
    IDiscordRestInteractionAPI interactionAPI) : CommandGroup
{
    [Command("publish")]
    [RoleAuthorize("Publisher")]
    [SuppressInteractionResponse(true)]
    [Description("Abre uma modal com as opções de publicação de um novo lançamento")]
    public async Task<IResult> Publish()
    {
        if (context is not InteractionContext interactionContext)
            return Result.FromSuccess();

        var modal = new ModalBuilder(nameof(PublishInteractions.PublishAsync), "Publicar novo lançamento")
            .AddField(fieldName: "link", label: "Link do capítulo")
            .AddField(fieldName: "title", label: "Nome do mangá")
            .AddField(fieldName: "chapterName", label: "Nome do capítulo", isRequired: false)
            .AddField(fieldName: "chapterInfo", label: "Numero do capítulo $ Volume")
            .AddField(fieldName: "message", label: "Mensagem de postagem", isRequired: false, TextInputStyle.Paragraph)
            .Create();

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
