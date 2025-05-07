using BotDeScans.App.Builders;
using BotDeScans.App.Extensions;
using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services.Discord;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
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


    // todo
    //[Modal(nameof(UpdateAsync))]
    //[Description("Atualiza dados da obra")]
    //public async Task<IResult> UpdateAsync(
    //string name,
    //string? role,
    //string state)
    //{
    //    var title = await databaseContext.Titles.AsNoTracking()
    //        .FirstOrDefaultAsync(x => x.Id == int.Parse(state));

    //    if (title is null)
    //        return await feedbackService.SendContextualWarningAsync(
    //            "Obra não encontrada.",
    //            ct: CancellationToken);

    //    ulong? roleId = null;
    //    if (string.IsNullOrEmpty(role) is false)
    //    {
    //        var roleResult = await rolesService.GetRoleFromGuildAsync(role!, CancellationToken);
    //        if (roleResult.IsFailed)
    //            return await feedbackService.SendContextualEmbedAsync(
    //            embed: EmbedBuilder.CreateErrorEmbed(roleResult.ToResult()),
    //            ct: CancellationToken);

    //        roleId = roleResult.Value.ID.Value;
    //    }

    //    var newTitle = title with { Name = name, DiscordRoleId = roleId };
    //    var validatioResult = await validator.ValidateAsync(title);
    //    if (validatioResult.IsValid is false)
    //        return await feedbackService.SendContextualEmbedAsync(
    //            embed: EmbedBuilder.CreateErrorEmbed(validatioResult.ToResult()),
    //            ct: CancellationToken);

    //    databaseContext.Update(newTitle);
    //    await databaseContext.SaveChangesAsync(CancellationToken);

    //    return await feedbackService.SendContextualEmbedAsync(
    //        embed: EmbedBuilder.CreateSuccessEmbed("Obra atualizada com sucesso!"),
    //        ct: CancellationToken);
    //}
}
