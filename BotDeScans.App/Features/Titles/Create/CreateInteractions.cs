using BotDeScans.App.Builders;
using BotDeScans.App.Extensions;
using BotDeScans.App.Infra;
using BotDeScans.App.Models;
using BotDeScans.App.Services.Discord;
using FluentValidation;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Results;
using System.ComponentModel;
namespace BotDeScans.App.Features.Titles.Create;

public class CreateInteractions(
    DatabaseContext databaseContext,
    FeedbackService feedbackService,
    RolesService rolesService,
    IValidator<Title> validator) : InteractionGroup
{
    [Modal(nameof(CreateAsync))]
    [Description("Cadastra nova obra")]
    public async Task<IResult> CreateAsync(
    string name,
    string? role)
    {
        ulong? roleId = null;
        if (string.IsNullOrEmpty(role) is false)
        {
            var roleResult = await rolesService.GetRoleFromGuildAsync(role!, CancellationToken);
            if (roleResult.IsFailed)
                return await feedbackService.SendContextualEmbedAsync(
                embed: EmbedBuilder.CreateErrorEmbed(roleResult.ToResult()),
                ct: CancellationToken);

            roleId = roleResult.Value.ID.Value;
        }

        var title = new Title(name, roleId);
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
}
