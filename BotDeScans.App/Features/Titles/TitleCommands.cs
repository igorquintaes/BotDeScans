using BotDeScans.App.Attributes;
using BotDeScans.App.Builders;
using BotDeScans.App.Features.Titles.Create;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System.ComponentModel;
namespace BotDeScans.App.Features.Titles;

[Group("title")]
public class TitleCommands(
    IOperationContext context,
    IDiscordRestInteractionAPI interactionAPI) : CommandGroup
{
    [Command("create")]
    [RoleAuthorize("Publisher")]
    [SuppressInteractionResponse(true)]
    [Description("Abre uma modal com as opções para cadastrar uma nova obra.")]
    public async Task<IResult> Create()
    {
        if (context is not InteractionContext interactionContext)
            return Result.FromSuccess();

        var modal = new ModalBuilder(nameof(CreateInteractions.CreateAsync), "Cadastrar nova obra")
            .AddField(fieldName: "name", label: "Nome da obra")
            .AddField(fieldName: "role", label: "Cargo do Discord para notificação", isRequired: false)
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
