using BotDeScans.App.Attributes;
using BotDeScans.App.Builders;
using BotDeScans.App.Features.Titles.Create;
using BotDeScans.App.Features.Titles.Update;
using BotDeScans.App.Infra;
using BotDeScans.App.Models;
using BotDeScans.App.Services;
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

        var modal = new ModalBuilder(nameof(UpdateInteractions.UpdateAsync), "Atualizar Obra")
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

    [Command("references-list")]
    [RoleAuthorize("Staff")]
    [Description("Adiciona ou atualiza referências externas para a obra.")]
    public async Task<IResult> References(
        [AutocompleteProvider(AutocompleteTitles.Id)]
        [Description("Nome da obra")]
        string titleName)
    {
        // todo: pagination based on titles length (characters quantity) - discord api
        var title = await databaseContext.Titles
            .Where(x => x.Name == titleName)
            .Include(x => x.References)
            .FirstOrDefaultAsync();

        if (title is null)
            return await feedbackService.SendContextualWarningAsync(
                "Obra não encontrada.",
                ct: CancellationToken);

        if (title.References.Count == 0)
            return await feedbackService.SendContextualWarningAsync(
                "Obra sem referências cadastradas.",
                ct: CancellationToken);

        var titlesListAsText = title.References
            .Select((x, index) => new { Number = index + 1, x.Key, x.Value })
            .Select(x => string.Format("{0}. {1}{2}{3}{2}", x.Number, x.Key.ToString(), Environment.NewLine, x.Value));

        return await feedbackService.SendContextualEmbedAsync(
            embed: EmbedBuilder.CreateSuccessEmbed(
                title: title.Name,
                description: string.Join(Environment.NewLine, titlesListAsText)),
            ct: CancellationToken);
    }

    [Command("references-update")]
    [RoleAuthorize("Publisher")]
    [Description("Adiciona ou atualiza referências externas para a obra.")]
    public async Task<IResult> References(
        [AutocompleteProvider(AutocompleteTitles.Id)]
        [Description("Nome da obra")]
        string titleName,
        [Description("Nome da referência")]
        ExternalReference referenceName,
        [Description("Valor da referência")]
        string referenceValue)
    {
        var title = await databaseContext.Titles
            .Where(x => x.Name == titleName)
            .FirstOrDefaultAsync();

        if (title is null)
            return await feedbackService.SendContextualWarningAsync(
                "Obra não encontrada.",
                ct: CancellationToken);

        var referenceValueParsedResult = referenceName switch
        {
            ExternalReference.MangaDex => mangaDexService.GetTitleIdFromUrl(referenceValue),
            _ => FluentResults.Result.Fail("Referência inválida")
        };

        if (referenceValueParsedResult.IsFailed)
            return await feedbackService.SendContextualEmbedAsync(
                embed: EmbedBuilder.CreateErrorEmbed(referenceValueParsedResult.ToResult()),
                ct: CancellationToken);

        var reference = await databaseContext.TitleReferences
            .AsNoTracking()
            .Where(x => x.TitleId == title.Id && x.Key == referenceName)
            .FirstOrDefaultAsync();

        if (reference is null)
        {
            var newReference = new TitleReference { Key = referenceName, Value = referenceValueParsedResult.Value, Title = title };

            await databaseContext.AddAsync(newReference);
            await databaseContext.SaveChangesAsync(CancellationToken);
        }
        else
        {
            var updatedReference = reference with { Value = referenceValueParsedResult.Value };
            databaseContext.Update(updatedReference);
            await databaseContext.SaveChangesAsync(CancellationToken);
        }

        return await feedbackService.SendContextualEmbedAsync(
            embed: EmbedBuilder.CreateSuccessEmbed(title: "Referência atualizada."),
            ct: CancellationToken);

    }
}
