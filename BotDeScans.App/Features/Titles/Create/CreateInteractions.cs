using BotDeScans.App.Builders;
using BotDeScans.App.Extensions;
using BotDeScans.App.Infra;
using BotDeScans.App.Models;
using FluentValidation;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Results;
using System.ComponentModel;

namespace BotDeScans.App.Features.Titles.Create;

public class CreateInteractions(
    IOperationContext context,
    DatabaseContext databaseContext,
    FeedbackService feedbackService,
    IValidator<Title> validator) : InteractionGroup
{
    [Modal(nameof(CreateAsync))]
    [Description("Cadastra novo título")]
    public async Task<IResult> CreateAsync(
    string name,
    string? role)
    {
        if (context is not InteractionContext interactionContext)
            return Result.FromSuccess();

        var title = new Title(name, role);
        var validatioResult = await validator.ValidateAsync(title);
        if (validatioResult.IsValid is false)
            return await feedbackService.SendEmbedAsync(
                channel: interactionContext.Interaction.Channel!.Value.ID!.Value,
                embed: EmbedBuilder.CreateErrorEmbed(validatioResult.ToResult()),
                ct: CancellationToken);

        await databaseContext.AddAsync(title, CancellationToken);
        await databaseContext.SaveChangesAsync(CancellationToken);

        return await feedbackService.SendEmbedAsync(
            channel: interactionContext.Interaction.Channel!.Value.ID!.Value,
            embed: EmbedBuilder.CreateSuccessEmbed("Título cadastrado com sucesso!"),
            ct: CancellationToken);
    }
}
