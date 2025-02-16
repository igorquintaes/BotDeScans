using BotDeScans.App.Builders;
using BotDeScans.App.Extensions;
using BotDeScans.App.Infra;
using BotDeScans.App.Services.Discord;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Interactivity;
using Remora.Results;
using System.ComponentModel;
using static BotDeScans.App.Features.Publish.PublishState;
namespace BotDeScans.App.Features.Publish.Discord;

public class PublishInteractions(
    DatabaseContext databaseContext,
    IOperationContext context,
    PublishHandler publishHandler,
    PublishState publishState,
    ExtendedFeedbackService feedbackService,
    MessageService messageService,
    IValidator<Info> validator) : InteractionGroup
{
    [Modal(nameof(PublishAsync))]
    [Description("Publica novo lançamento")]
    public async Task<IResult> PublishAsync(
        string driveUrl,
        string? chapterName,
        string chapterNumber,
        string? chapterVolume,
        string? message,
        string state)
    {
        if (context is not InteractionContext interactionContext)
            return Result.FromSuccess();

        var interactionChannel = interactionContext.Interaction.Channel!.Value.ID!.Value;

        var info = new Info(driveUrl, chapterName, chapterNumber, chapterVolume, message);
        var infoValidationResult = validator.Validate(info);
        if (infoValidationResult.IsValid is false)
            return await feedbackService.SendEmbedAsync(
                channel: interactionChannel,
                embed: EmbedBuilder.CreateErrorEmbed(infoValidationResult.ToResult()),
                ct: CancellationToken);

        var titleId = int.Parse(state);
        var title = await databaseContext.Titles.Include(x => x.References).FirstOrDefaultAsync(x => x.Id == titleId, CancellationToken);
        if (title is null)
            return await feedbackService.SendEmbedAsync(
                channel: interactionChannel,
                embed: EmbedBuilder.CreateErrorEmbed(description: "Obra não encontrada"),
                ct: CancellationToken);

        publishState.Title = title;
        publishState.ReleaseInfo = info;

        var publishTrackingMessage = await feedbackService.SendContextualEmbedAsync(new Embed("Iniciando..."),  ct: CancellationToken);
        if (publishTrackingMessage.IsSuccess is false)
            return publishTrackingMessage;

        var result = await publishHandler.HandleAsync(
            feedbackFunc: () => messageService.UpdatePublishTrackingMessageAsync(
                publishTrackingMessage, 
                interactionContext, 
                CancellationToken), 
            CancellationToken);

        if (result.IsFailed)
            return await feedbackService.SendEmbedAsync(
                channel: interactionChannel,
                embed: EmbedBuilder.CreateErrorEmbed(result),
                ct: CancellationToken);

        return await messageService.PublishReleaseAsync(
            interactionContext,
            result.Value,
            CancellationToken);
    }
}