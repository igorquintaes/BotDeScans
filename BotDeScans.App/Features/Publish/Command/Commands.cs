﻿using BotDeScans.App.Attributes;
using BotDeScans.App.Builders;
using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Services.Discord.Autocomplete;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System.ComponentModel;

namespace BotDeScans.App.Features.Publish.Command;

public class Commands(
    IOperationContext context,
    IDiscordRestInteractionAPI interactionAPI) : CommandGroup
{
    [Command("publish")]
    [RoleAuthorize("Publisher")]
    [SuppressInteractionResponse(true)]
    [Description("Abre uma modal com as opções de publicação de um novo lançamento")]
    public async Task<IResult> ExecuteAsync(
        [AutocompleteProvider(AutocompleteTitles.ID)]
        [Description("Nome da obra")]
        int title)
    {
        if (context is not InteractionContext interactionContext)
            return Result.FromSuccess();

        var modal = new ModalBuilder(Interactions.MODAL_NAME, "Publicar novo lançamento")
            .AddField(fieldName: "driveUrl", label: "Link do capítulo")
            .AddField(fieldName: "chapterName", label: "Nome do capítulo", isRequired: false)
            .AddField(fieldName: "chapterNumber", label: "Número do capítulo")
            .AddField(fieldName: "chapterVolume", label: "Número do Volume", isRequired: false)
            .AddField(fieldName: "message", label: "Mensagem de postagem", isRequired: false, TextInputStyle.Paragraph)
            .CreateWithState(title.ToString());

        return await interactionAPI.CreateInteractionResponseAsync
        (
            interactionContext.Interaction.ID,
            interactionContext.Interaction.Token,
            new InteractionResponse(InteractionCallbackType.Modal, modal),
            ct: CancellationToken
        );
    }
}
