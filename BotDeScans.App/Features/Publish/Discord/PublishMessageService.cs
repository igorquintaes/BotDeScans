using BotDeScans.App.Builders;
using BotDeScans.App.Extensions;
using Microsoft.Extensions.Configuration;
using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reflection;
using static BotDeScans.App.Features.Publish.PublishState;

namespace BotDeScans.App.Features.Publish.Discord;

[ExcludeFromCodeCoverage]
public class PublishMessageService(
    PublishState publishState,
    PublishReplacerService publishReplacerService,
    IFeedbackService feedbackService,
    IConfiguration configuration,
    IDiscordRestInteractionAPI discordRestInteractionAPI,
    IDiscordRestChannelAPI discordRestChannelAPI)
{
    private Result<IMessage>? trackingMessage = null;

    public virtual async Task<FluentResults.Result> UpdateTrackingMessageAsync(
        IOperationContext context,
        CancellationToken cancellationToken)
    {
        var interactionContext = context as InteractionContext;
        var steps = publishState.Steps!;
        var embed = new Embed(steps.Header, Description: steps.Details, Colour: steps.ColorStatus);

        trackingMessage = trackingMessage is null
            ? await feedbackService.SendContextualEmbedAsync(embed, ct: cancellationToken)
            : await discordRestInteractionAPI.EditFollowupMessageAsync(
                trackingMessage.Value.Entity.Author.ID,
                interactionContext!.Interaction.Token,
                messageID: trackingMessage.Value.Entity.ID,
                embeds: new List<Embed> { embed },
                ct: cancellationToken);

        return trackingMessage.Value.IsSuccess is true
            ? FluentResults.Result.Ok()
            : FluentResults.Result
                .Fail("Error to update Discord message.")
                .WithError(trackingMessage.Value.Error.Message);
    }

    public virtual async Task<Result<IMessage>> ErrorReleaseMessageAsync(
        IOperationContext context,
        FluentResults.Result errorResult,
        CancellationToken cancellationToken)
    {
        var interactionContext = context as InteractionContext;
        var channel = interactionContext!.Interaction.Channel!.Value.ID!.Value;
        var embed = EmbedBuilder.CreateErrorEmbed(errorResult);

        return await feedbackService.SendEmbedAsync(channel, embed, ct: cancellationToken);
    }

    public virtual async Task<Result<IMessage>> SuccessReleaseMessageAsync(
        IOperationContext context,
        string content,
        CancellationToken cancellationToken)
    {
        var releaseChannel = new Snowflake(configuration.GetRequiredValue<ulong>("Discord:ReleaseChannel"));
        var coverFileName = Path.GetFileName(publishState.InternalData.CoverFilePath);
        using var cover = new FileStream(publishState.InternalData.CoverFilePath, FileMode.Open);

        return await discordRestChannelAPI.CreateMessageAsync(
            channelID: releaseChannel,
            content: content,
            embeds: new[] { PublishEmbed(context, coverFileName) },
            attachments: new[] { OneOf<FileData, IPartialAttachment>.FromT0(new FileData(coverFileName, cover)) },
            components: new[] { new ActionRowComponent([PromotedButton]) },
            ct: cancellationToken);
    }

    private Embed PublishEmbed(
        IOperationContext context,
        string coverFileName)
    {
        var interactionContext = context as InteractionContext;
        var message = string.IsNullOrWhiteSpace(publishState.ReleaseInfo.Message)
            ? string.Empty
            : publishReplacerService.Replace(publishState.ReleaseInfo.Message);

        return new(
            Title: $"#{publishState.ReleaseInfo.ChapterNumber} {publishState.Title.Name}",
            Image: new EmbedImage($"attachment://{coverFileName}"),
            Description: message,
            Colour: Color.Green,
            Fields: CreatePublishLinkFields(),
            Author: new EmbedAuthor(
                Name: interactionContext!.GetUserName(),
                IconUrl: interactionContext!.GetUserAvatarUrl()));
    }

    private List<EmbedField> CreatePublishLinkFields()
        => typeof(Links)
            .GetProperties()
            .Where(property => Attribute.IsDefined(property, typeof(DescriptionAttribute)))
            .Select(property => new
            {
                Label = property.GetCustomAttribute<DescriptionAttribute>()!.Description,
                Link = property.GetValue(publishState.ReleaseLinks, null)?.ToString()
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Link))
            .Select(x => new EmbedField(x.Label, $":white_check_mark:  [Acesse]({x.Link})", true))
            .ToList();

    private static readonly ButtonComponent PromotedButton = new(
            ButtonComponentStyle.Link,
            Label: "Escola de Scans",
            URL: "https://www.youtube.com/c/EscoladeScans");
}
