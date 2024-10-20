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
namespace BotDeScans.App.Services.Discord.Commands;

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

        var modal = new ModalBuilder(nameof(Publish), "Publicar novo lançamento")
            .AddField("link", "Link do capítulo")
            .AddField("title", "Nome do mangá")
            .AddField("chapterName", "Nome do capítulo", false)
            .AddField("chapterNumberAndVolume", "NumeroCap $ Volume")
            .AddField("message", "Mensagem de postagem", false, TextInputStyle.Paragraph)
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
