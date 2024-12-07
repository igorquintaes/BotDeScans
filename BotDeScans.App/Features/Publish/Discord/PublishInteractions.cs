using BotDeScans.App.Builders;
using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Infra;
using BotDeScans.App.Services.Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Interactivity;
using Remora.Rest.Core;
using Remora.Results;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using static BotDeScans.App.Features.Publish.PublishState;
namespace BotDeScans.App.Features.Publish.Discord;

public class PublishInteractions(
    DatabaseContext databaseContext,
    IOperationContext context,
    IConfiguration configuration,
    PublishHandler publishHandler,
    PublishState publishState,
    ExtendedFeedbackService feedbackService,
    IDiscordRestInteractionAPI discordRestInteractionAPI,
    IDiscordRestChannelAPI discordRestChannelAPI) : InteractionGroup
{
    [Modal(nameof(PublishAsync))]
    [Description("Publica novo lançamento")]
    public async Task<IResult> PublishAsync(
        string driveUrl,
        string? chapterName,
        string chapterNumber,
        string chapterVolume,
        string? message,
        string state)
    {
        if (context is not InteractionContext interactionContext)
            return Result.FromSuccess();

        var title = await databaseContext.Titles.Include(x => x.References).FirstOrDefaultAsync(x => x.Id == int.Parse(state));
        if (title is null)
            return await feedbackService.SendEmbedAsync(
                channel: interactionContext.Interaction.Channel!.Value.ID!.Value,
                embed: EmbedBuilder.CreateErrorEmbed(description: "Obra não encontrada"),
                ct: CancellationToken);

        publishState.Title = title;
        publishState.ReleaseInfo.DownloadUrl = driveUrl;
        publishState.ReleaseInfo.ChapterName = chapterName;
        publishState.ReleaseInfo.ChapterNumber = chapterNumber;
        publishState.ReleaseInfo.ChapterVolume = chapterVolume;
        publishState.ReleaseInfo.Message = message;

        var publishStateMessage = await feedbackService.SendContextualEmbedAsync(new Embed("Iniciando..."), ct: CancellationToken);
        if (publishStateMessage.IsSuccess is false)
            return publishStateMessage;

        var result = await publishHandler.HandleAsync(() => Feedback(publishStateMessage, interactionContext), CancellationToken);
        if (result.IsFailed)
            return await feedbackService.SendEmbedAsync(
                channel: interactionContext.Interaction.Channel!.Value.ID!.Value,
                embed: EmbedBuilder.CreateErrorEmbed(result),
                ct: CancellationToken);

        var linkFields = CreatePublishLinkFields();
        var releaseChannel = new Snowflake(configuration.GetRequiredValue<ulong>("Discord:ReleaseChannel"));
        var coverFileName = Path.GetFileName(publishState.InternalData.CoverFilePath);
        using var cover = new FileStream(publishState.InternalData.CoverFilePath, FileMode.Open);

        var embed = new Embed(
            Title: $"#{publishState.ReleaseInfo.ChapterNumber} {publishState.Title.Name}",
            Image: new EmbedImage($"attachment://{coverFileName}"),
            Description: publishState.ReleaseInfo.Message ?? string.Empty,
            Colour: Color.Green,
            Fields: linkFields,
            Author: new EmbedAuthor(
                Name: interactionContext.GetUserName(),
                IconUrl: interactionContext.GetUserAvatarUrl()));

        var promotedButton = new ButtonComponent(
            ButtonComponentStyle.Link,
            Label: "Escola de Scans",
            URL: "https://www.youtube.com/c/EscoladeScans");

        // todo: podemos criar um builder para casos assim
        return await discordRestChannelAPI.CreateMessageAsync(
            channelID: releaseChannel,
            content: result.Value,
            embeds: new[] { embed },
            attachments: new[] { OneOf<FileData, IPartialAttachment>.FromT0(new FileData(coverFileName, cover)) },
            components: new[] { new ActionRowComponent([promotedButton]) },
            ct: CancellationToken);
    }

    // todo: podemos verificar se faz sentido mudar para botões... e se é possível customizar width
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

    private async Task<FluentResults.Result> Feedback(
        Result<IMessage> publishStateMessage,
        InteractionContext interactionContext)
    {
        var tasks = new StepsInfo(publishState.Steps);
        var embed = new Embed(
            Title: tasks.Header,
            Description: tasks.Details,
            Colour: tasks.ColorStatus);

        var updateMessageResult = await discordRestInteractionAPI.EditFollowupMessageAsync(
            publishStateMessage.Entity.Author.ID,
            interactionContext.Interaction.Token,
            messageID: publishStateMessage.Entity.ID,
            embeds: new List<Embed> { embed },
            ct: CancellationToken);

        return updateMessageResult.IsSuccess is true
            ? FluentResults.Result.Ok()
            : FluentResults.Result
                .Fail("Error while updating message in discord.")
                .WithError(updateMessageResult.Error.Message);
    }
}
