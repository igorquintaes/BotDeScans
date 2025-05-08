using BotDeScans.App.Attributes;
using BotDeScans.App.Builders;
using BotDeScans.App.Features.Titles.Create;
using BotDeScans.App.Infra;
using BotDeScans.App.Services.Discord.Autocomplete;
using BotDeScans.App.Services.MangaDex;
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
namespace BotDeScans.App.Features.Titles;

[Group("title")]
public class TitleCommands(
    IOperationContext context,
    IDiscordRestInteractionAPI interactionAPI,
    FeedbackService feedbackService,
    DatabaseContext databaseContext,
    MangaDexService mangaDexService) : CommandGroup
{
    [Command("create")]
    [RoleAuthorize("Publisher")]
    [SuppressInteractionResponse(true)]
    [Description("Abre uma modal com as opções para cadastrar uma nova obra")]
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

    [Command("list")]
    [RoleAuthorize("Staff")]
    [Description("Obtém uma lista com todos as obras cadastradas")]
    public async Task<IResult> List()
    {
        // todo: pagination based on titles length (characters quantity) - discord api
        var titles = await databaseContext.Titles.ToListAsync();
        if (titles.Count == 0)
            return await feedbackService.SendContextualWarningAsync(
                "Não há obras cadastradas.",
                ct: CancellationToken);

        var titlesListAsText = titles
            .Select((x, index) => new { Number = index + 1, x.Name })
            .Select(x => string.Format("{0}. {1}", x.Number, x.Name));

        return await feedbackService.SendContextualEmbedAsync(
            embed: EmbedBuilder.CreateSuccessEmbed(
                title: "Lista de obras",
                description: string.Join(Environment.NewLine, titlesListAsText)),
            ct: CancellationToken);
    }

    [Command("update")]
    [RoleAuthorize("Publisher")]
    [SuppressInteractionResponse(true)]
    [Description("Atualiza dados da obra")]
    public async Task<IResult> Update(
        [AutocompleteProvider(AutocompleteTitles.Id)]
        [Description("Nome da obra")]
        string titleName)
    {
        if (context is not InteractionContext interactionContext)
            return Result.FromSuccess();

        var title = await databaseContext.Titles.FirstOrDefaultAsync(x => x.Name == titleName);
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
