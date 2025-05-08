using BotDeScans.App.Builders;
using BotDeScans.App.Extensions;
using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services.Discord;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Results;
using System.ComponentModel;
namespace BotDeScans.App.Features.Titles.Update;

public class UpdateInteractions(
    DatabaseContext databaseContext,
    FeedbackService feedbackService,
    RolesService rolesService,
    IValidator<Title> validator) : InteractionGroup
{
   [Modal(nameof(UpdateAsync))]
   [Description("Atualiza dados da obra")]
    public async Task<IResult> UpdateAsync(
    string name,
    string? role,
    string state)
    {
        var titleId = int.Parse(state);
        var title = await databaseContext.Titles.SingleAsync(
            x => x.Id == titleId, 
            CancellationToken);

        ulong? roleId = null;
        if (string.IsNullOrWhiteSpace(role) is false)
        {
            var roleResult = await rolesService.GetRoleFromGuildAsync(role!, CancellationToken);
            if (roleResult.IsFailed)
                return await feedbackService.SendContextualEmbedAsync(
                embed: EmbedBuilder.CreateErrorEmbed(roleResult.ToResult()),
                ct: CancellationToken);

            roleId = roleResult.Value.ID.Value;
        }

        title.Name = name;
        title.DiscordRoleId = roleId;

        var validatioResult = await validator.ValidateAsync(title);
        if (validatioResult.IsValid is false)
            return await feedbackService.SendContextualEmbedAsync(
                embed: EmbedBuilder.CreateErrorEmbed(validatioResult.ToResult()),
                ct: CancellationToken);

        await databaseContext.SaveChangesAsync(CancellationToken);

        return await feedbackService.SendContextualEmbedAsync(
            embed: EmbedBuilder.CreateSuccessEmbed("Obra atualizada com sucesso!"),
            ct: CancellationToken);
    }
}
