using BotDeScans.App.Builders;
using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Models;
using Microsoft.Extensions.Configuration;
using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using System.Drawing;
using System.Reflection;

namespace BotDeScans.App.Features.Publish.Interaction;

public class DiscordPublisher(
    IOperationContext context,
    TextReplacer textReplacer,
    IFeedbackService feedbackService,
    IConfiguration configuration,
    IDiscordRestInteractionAPI discordRestInteractionAPI,
    IDiscordRestChannelAPI discordRestChannelAPI)
{
    private readonly SemaphoreSlim _trackingLock = new(1, 1);

    public virtual async Task<FluentResults.Result<State>> SynchronizedUpdateTrackingMessageAsync(
        State state,
        CancellationToken cancellationToken)
    {
        await _trackingLock.WaitAsync(cancellationToken);
        try
        {
            return await UpdateTrackingMessageAsync(state, cancellationToken);
        }
        finally
        {
            _trackingLock.Release();
        }
    }

    public virtual async Task<FluentResults.Result<State>> UpdateTrackingMessageAsync(
        State state,
        CancellationToken cancellationToken)
    {
        var interactionContext = context as InteractionContext;
        var steps = state.Steps;
        var embed = new Embed(steps.MessageStatus, Description: steps.Details, Colour: steps.ColorStatus);
        var trackingMessage = state.TrackingMessage;

        var result = trackingMessage is null
            ? await feedbackService.SendContextualEmbedAsync(embed, ct: cancellationToken)
            : await discordRestInteractionAPI.EditFollowupMessageAsync(
                trackingMessage.AuthorId,
                interactionContext!.Interaction.Token,
                messageID: trackingMessage.MessageId,
                embeds: new List<Embed> { embed },
                ct: cancellationToken);

        if (result.IsSuccess is false)
            return FluentResults.Result
                .Fail("Error to update Discord message.")
                .WithError(result.Error.Message);

        var updatedState = state with
        {
            TrackingMessage = new TrackingMessage(
                result.Entity.Author.ID,
                result.Entity.ID)
        };

        return FluentResults.Result.Ok(updatedState);
    }

    public virtual async Task<IResult<IMessage>> ErrorReleaseMessageAsync(
        FluentResults.Result errorResult,
        CancellationToken cancellationToken)
    {
        var interactionContext = context as InteractionContext;
        var channel = interactionContext!.Interaction.Channel!.Value.ID!.Value;
        var embed = EmbedBuilder.CreateErrorEmbed(errorResult);

        return await feedbackService.SendEmbedAsync(channel, embed, ct: cancellationToken);
    }

    public virtual async Task<IResult<IMessage>> SuccessReleaseMessageAsync(
        State publishState,
        CancellationToken cancellationToken)
    {
        var interactionContext = context as InteractionContext;
        var releaseChannel = new Snowflake(configuration.GetRequiredValue<ulong>("Discord:ReleaseChannel"));
        var coverFileName = Path.GetFileName(publishState.CoverFilePath);
        using var cover = new FileStream(publishState.CoverFilePath, FileMode.Open);

        return await discordRestChannelAPI.CreateMessageAsync(
            channelID: releaseChannel,
            content: publishState.Pings!,
            embeds: new[] { PublishEmbed(interactionContext!, coverFileName, publishState) },
            attachments: new[] { OneOf<FileData, IPartialAttachment>.FromT0(new FileData(coverFileName, cover)) },
            components: new[] { new ActionRowComponent([PromotedButton]) },
            ct: cancellationToken);
    }

    private Embed PublishEmbed(
        InteractionContext interactionContext,
        string coverFileName,
        State publishState)
    {
        var message = string.IsNullOrWhiteSpace(publishState.ChapterInfo.Message)
            ? string.Empty
            : textReplacer.Replace(publishState.ChapterInfo.Message, publishState);

        return new(
            Title: $"#{publishState.ChapterInfo.ChapterNumber} {publishState.Title.Name}",
            Image: new EmbedImage($"attachment://{coverFileName}"),
            Description: message,
            Colour: Color.Green,
            Fields: CreatePublishLinkFields(publishState),
            Author: new EmbedAuthor(
                Name: interactionContext!.GetUserName(),
                IconUrl: interactionContext!.GetUserAvatarUrl()));
    }

    private static List<EmbedField> CreatePublishLinkFields(State publishState) => [.. typeof(State)
        .GetProperties()
        .Where(property => property.GetCustomAttribute<ReleaseLinkAttribute>() is not null)
        .Select(property => new
        {
            Label = property.GetCustomAttribute<ReleaseLinkAttribute>()!.Label,
            Link = property.GetValue(publishState, null)?.ToString()
        })
        .Where(x => !string.IsNullOrWhiteSpace(x.Link))
        .Select(x => new EmbedField(x.Label, $":white_check_mark:  [Acesse]({x.Link})", true))];

    private static readonly ButtonComponent PromotedButton = new(
        ButtonComponentStyle.Link,
        Label: "Escola de Scans",
        URL: "https://www.youtube.com/c/EscoladeScans");
}
