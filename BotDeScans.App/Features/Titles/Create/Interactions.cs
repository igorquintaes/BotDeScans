using BotDeScans.App.Builders;
using BotDeScans.App.Extensions;
using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services.Discord;
using FluentResults;
using FluentValidation;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Results;
using System.ComponentModel;

namespace BotDeScans.App.Features.Titles.Create;

public class Interactions(
    DatabaseContext databaseContext,
    FeedbackService feedbackService,
    RolesService rolesService,
    IValidator<Title> validator) : InteractionGroup
{
    public const string MODAL_NAME = "Titles.Create.Interaction";

    [Modal(MODAL_NAME)]
    [Description("Cadastra nova obra")]
    public async Task<IResult> ExecuteAsync(
        string name,
        string role)
    {
        // todo: o fluxo vai chamar duas vezes a api do discord aqui para roles,
        // uma vez para obter o id através de uma string (nome ou id)
        // outra vez para realizar a validação do objeto Title,
        // pois tem validação compartilhada com create/edit/publish.
        // O ideal aqui é criarmos regras do que validar ou não,
        // E inicialmente podemos, em complemento, criar um cache das roles.

        var roleResult = await GetDiscordRoleId(role);
        if (roleResult.IsFailed)
            return await feedbackService.SendContextualEmbedAsync(
                embed: EmbedBuilder.CreateErrorEmbed(roleResult.ToResult()),
                ct: CancellationToken);

        var title = new Title { Name = name, DiscordRoleId = roleResult.Value?.ID.Value };
        var validationResult = await validator.ValidateAsync(title);
        if (validationResult.IsValid is false)
            return await feedbackService.SendContextualEmbedAsync(
                embed: EmbedBuilder.CreateErrorEmbed(validationResult.ToResult()),
                ct: CancellationToken);

        await databaseContext.AddAsync(title, CancellationToken);
        await databaseContext.SaveChangesAsync(CancellationToken);

        return await feedbackService.SendContextualEmbedAsync(
            embed: EmbedBuilder.CreateSuccessEmbed("Obra cadastrada com sucesso!"),
            ct: CancellationToken);
    }

    private async Task<FluentResults.Result<IRole?>> GetDiscordRoleId(string roleRequest)
    {
        if (string.IsNullOrWhiteSpace(roleRequest))
            return FluentResults.Result.Ok<IRole?>(null);

        var roleResult = await rolesService.GetRoleFromGuildAsync(roleRequest, CancellationToken);
        if (roleResult.IsFailed)
            return roleResult.ToResult();

        return FluentResults.Result.Ok<IRole?>(roleResult.Value);
    }
}
