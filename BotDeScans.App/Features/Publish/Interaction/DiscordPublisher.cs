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

    private async Task<FluentResults.Result<State>> UpdateTrackingMessageAsync(
        State state,
        CancellationToken cancellationToken)
    {
        var interactionContext = context as InteractionContext;

        var steps = state.Steps;
        var trackingMessage = state.TrackingMessage;
        var embed = new Embed(steps.MessageStatus, Description: steps.Details, Colour: steps.ColorStatus);

        var remoraResult = trackingMessage is null
            ? await feedbackService.SendContextualEmbedAsync(embed, ct: cancellationToken)
            : await discordRestInteractionAPI.EditFollowupMessageAsync(
                    trackingMessage.AuthorId,
                    interactionContext!.Interaction.Token,
                    messageID: trackingMessage.MessageId,
                    embeds: new List<Embed> { embed },
                    ct: cancellationToken);

        return remoraResult.ToFluentResult(() => state with 
        { 
            TrackingMessage = new TrackingMessage(
                remoraResult.Entity.Author.ID, 
                remoraResult.Entity.ID) 
        });
    }

    public virtual async Task<IResult<IMessage>> ErrorReleaseMessageAsync(
        FluentResults.Result errorResult,
        CancellationToken cancellationToken)
    {
        errorResult.LogIfFailed();

        var interactionContext = context as InteractionContext;
        var channel = interactionContext!.Interaction.Channel!.Value.ID!.Value;
        var embed = EmbedBuilder.CreateErrorEmbed(errorResult);

        return await feedbackService.SendEmbedAsync(channel, embed, ct: cancellationToken);
    }

    public virtual async Task<IResult<IMessage>> SuccessReleaseMessageAsync(
        State publishState,
        CancellationToken cancellationToken)
    {
        // Image as Attachment
        using var cover = new FileStream(publishState.CoverFilePath, FileMode.Open);
        var coverFileName = Path.GetFileName(publishState.CoverFilePath);
        var fileData = new FileData(coverFileName, cover);
        var attachment = OneOf<FileData, IPartialAttachment>.FromT0(fileData);
        
        // Discord Context Data
        var interactionContext = context as InteractionContext;
        var releaseChannelId = configuration.GetRequiredValue<ulong>("Discord:ReleaseChannel");
        var releaseChannel = new Snowflake(releaseChannelId);

        // Content
        var promotedComponent = new ActionRowComponent([PromotedButton]);
        var embed = PublishEmbed(interactionContext!, coverFileName, publishState);
        var pingText = publishState.PingText;

        return await discordRestChannelAPI.CreateMessageAsync(
            channelID: releaseChannel,
            content: pingText!,
            embeds: new[] { embed },
            attachments: new[] { attachment },
            components: new[] { promotedComponent },
            ct: cancellationToken);
    }

    private Embed PublishEmbed(
        InteractionContext interactionContext,
        string coverFileName,
        State publishState)
    {
        var title = $"#{publishState.ChapterInfo.ChapterNumber} {publishState.Title.Name}";
        var image = new EmbedImage($"attachment://{coverFileName}");
        var message = string.IsNullOrWhiteSpace(publishState.ChapterInfo.Message) is false
            ? textReplacer.Replace(publishState.ChapterInfo.Message, publishState)
            : string.Empty;

        return new(Title: title,
                   Image: image,
                   Description: message!,
                   Colour: Color.Green,
                   Fields: CreatePublishLinkFields(publishState),
                   Author: interactionContext.GetAuthor());
    }

    private static EmbedField[] CreatePublishLinkFields(State publishState)
    {
        const string LINK_TEXT = ":white_check_mark:  [Acesse]({0})";

        return 
        [.. 
            typeof(State)
                .GetProperties()
                .Where(property => property.GetCustomAttribute<ReleaseLinkAttribute>() is not null
                                && property.GetValue(publishState, null) is not null)
                .Select(x => new EmbedField(
                    Name: x.GetCustomAttribute<ReleaseLinkAttribute>()!.Label,
                    Value: string.Format(LINK_TEXT, x.GetValue(publishState, null)!.ToString()),
                    IsInline: true))
        ];
    }

    private static readonly ButtonComponent PromotedButton = new(
        ButtonComponentStyle.Link,
        Label: "Escola de Scans",
        URL: "https://www.youtube.com/c/EscoladeScans");
}
