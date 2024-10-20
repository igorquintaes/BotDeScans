using BotDeScans.App.Builders;
using BotDeScans.App.Enums;
using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Discord.Commands;
using BotDeScans.App.Services.Publish;
using Microsoft.Extensions.Configuration;
using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Interactivity;
using Remora.Results;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using static BotDeScans.App.Services.Publish.PublishState;
namespace BotDeScans.App.Services.Discord.Interactions;

// Todo: Criar cadeia de modals
// Edit: A API do Discord não permite uma modal chamar outra, F
//       A ideia é criar um embed paginador com botões que abrem e executam modais.
public class PublishInteractions(
    ExtendedFeedbackService feedbackService,
    PublishState publishState,
    IOperationContext context,
    PublishService publishService,
    IConfiguration configuration,
    IDiscordRestInteractionAPI discordRestInteractionAPI,
    IDiscordRestChannelAPI discordRestChannelAPI) : InteractionGroup
{
    [Modal(nameof(PublishCommands.Publish))]
    [Description("Publica novo lançamento")]
    public async Task<IResult> OnModalSubmitAsync(
        string link,
        string title,
        string? chapterName,
        string chapterNumberAndVolume,
        string? message)
    {
        if (context is not InteractionContext interactionContext)
            return Result.FromSuccess();

        publishState.Info.Link = link;
        publishState.Info.DisplayTitle = title;
        publishState.Info.ChapterName = chapterName;
        publishState.Info.ChapterNumber = chapterNumberAndVolume.Split('$').First().Trim();
        publishState.Info.ChapterVolume = chapterNumberAndVolume.Contains('$') ? chapterNumberAndVolume.Split('$').Last().Trim() : null;
        publishState.Info.Message = message;

        var discordCommandChannel = interactionContext.Interaction.Channel!.Value.ID!.Value;
        var discordReleaseChannel = new Remora.Rest.Core.Snowflake(configuration.GetRequiredValue<ulong>("Discord:ReleaseChannel"));
        var contextualMessage = await feedbackService.SendContextualEmbedAsync(new Embed(Description: $"Validando dados do link..."), ct: CancellationToken);

        var preValidationResult = await publishService.ValidateBeforeFilesManagementAsync(CancellationToken);
        if (preValidationResult.IsFailed)
            return await preValidationResult.PostErrorOnDiscord(feedbackService, CancellationToken);

        var publishPingResult = await publishService.GetPublishPingAsync(title, CancellationToken);
        if (publishPingResult.IsFailed)
            return await feedbackService.SendEmbedAsync(
                channel: discordCommandChannel,
                embed: EmbedBuilder.CreateErrorEmbed(publishPingResult),
                ct: CancellationToken);

        chainCompletedState = false;
        var taskExecuteManagementChain = ExecutePublishChain(StepType.Management);
        var taskDisplayManagementExecution = UpdateExecutionOnDiscord(contextualMessage, interactionContext);
        var managementResult = FluentResults.Result.Merge(await Task.WhenAll(taskExecuteManagementChain, taskDisplayManagementExecution));
        if (managementResult.IsFailed)
            return await feedbackService.SendEmbedAsync(
                channel: discordCommandChannel,
                embed: EmbedBuilder.CreateErrorEmbed(managementResult),
                ct: CancellationToken);

        var posValidationResult = await publishService.ValidateAfterFilesManagementAsync(CancellationToken);
        if (posValidationResult.IsFailed)
            return await posValidationResult.PostErrorOnDiscord(feedbackService, CancellationToken);

        chainCompletedState = false;
        var taskExecutePublishChain = ExecutePublishChain(StepType.Publish);
        var taskDisplayPublishExecution = UpdateExecutionOnDiscord(contextualMessage, interactionContext);
        var publishResult = FluentResults.Result.Merge(await Task.WhenAll(taskExecutePublishChain, taskDisplayPublishExecution));
        if (publishResult.IsFailed)
            return await feedbackService.SendEmbedAsync(
                channel: discordCommandChannel,
                embed: EmbedBuilder.CreateErrorEmbed(publishResult),
                ct: CancellationToken);

        var fields = CreatePublishEmbedLinks();
        var embed = new Embed(
            Title: $"#{publishState.Info.ChapterNumber} {publishState.Info.DisplayTitle}",
            Image: new EmbedImage($"attachment://{Path.GetFileName(publishState.InternalData.CoverFilePath)}"),
            Description: publishState.Info.Message ?? string.Empty,
            Colour: Color.Green,
            Fields: fields,
            Author: new EmbedAuthor(
                Name: interactionContext.GetUserName(),
                IconUrl: interactionContext.GetUserAvatarUrl()));

        var promotedButton = new ButtonComponent(
            ButtonComponentStyle.Link,
            Label: "Escola de Scans",
            URL: "https://www.youtube.com/c/EscoladeScans");

        using var cover = new FileStream(publishState.InternalData.CoverFilePath, FileMode.Open);
        var coverFileName = Path.GetFileName(publishState.InternalData.CoverFilePath);

        // todo: podemos criar um builder para casos assim
        return await discordRestChannelAPI.CreateMessageAsync
        (
            discordReleaseChannel,
            content: publishPingResult.Value,
            embeds: new[] { embed },
            attachments: new[] { OneOf<FileData, IPartialAttachment>.FromT0(new FileData(coverFileName, cover)) },
            components: new[] { new ActionRowComponent(new[] { promotedButton }) },
            ct: CancellationToken
        );
    }

    private List<EmbedField> CreatePublishEmbedLinks() 
        => typeof(ReleaseLinks)
            .GetProperties()
            .Where(property => Attribute.IsDefined(property, typeof(DescriptionAttribute)))
            .Select(property => new
            {
                Label = property.GetCustomAttribute<DescriptionAttribute>()!.Description,
                Link = property.GetValue(publishState.Links, null)?.ToString()
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Link))
            .Select(x => new EmbedField(x.Label, $":white_check_mark:  [clique aqui]({x.Link})", true))
            .ToList();

    private bool chainCompletedState = false;
    private Dictionary<StepEnum, StepStatus> currentStepState = [];

    private async Task<FluentResults.Result> ExecutePublishChain(StepType stepType)
    {
        var result = await publishService.RunAsync(stepType, CancellationToken);
        chainCompletedState = true;
        return result;
    }

    private async Task<FluentResults.Result> UpdateExecutionOnDiscord(
        Result<IMessage> contextualMessage,
        InteractionContext interactionContext)
    {
        currentStepState = publishState.Steps.ToDictionary(
            entry => entry.Key, 
            entry => entry.Value);

        while (chainCompletedState is false)
        {
            var stepsWereUpdated = publishState.Steps.Any(entry => currentStepState[entry.Key] != entry.Value);
            if (stepsWereUpdated)
            {
                currentStepState = publishState.Steps.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value);

                contextualMessage = await EmbedBuilder.HandleTasksAndUpdateMessage(
                    steps: publishState.Steps,
                    interactionContext: interactionContext,
                    reply: contextualMessage,
                    discordRestInteractionAPI,
                    cancellationToken: CancellationToken);
            }

            Thread.Sleep(300);
        }

        await EmbedBuilder.HandleTasksAndUpdateMessage(
            steps: publishState.Steps,
            interactionContext: interactionContext,
            reply: contextualMessage,
            discordRestInteractionAPI,
            cancellationToken: CancellationToken);

        return FluentResults.Result.Ok();
    }
}
