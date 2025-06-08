using BotDeScans.App.Features.Publish.Interaction.Steps;
using Remora.Discord.Interactivity;
using Remora.Results;
using Serilog;
using System.ComponentModel;

namespace BotDeScans.App.Features.Publish.Interaction;

public class Interactions(
    DiscordPublisher discordPublisher,
    StepsService stepsService,
    State interactionState,
    Handler handler) : InteractionGroup
{
    [Modal(nameof(PublishAsync))]
    [Description("Publica novo lançamento")]
    public async Task<IResult> PublishAsync(
        string driveUrl,
        string chapterName,
        string chapterNumber,
        string chapterVolume,
        string message,
        string state)
    {
        interactionState.ChapterInfo = new(driveUrl, chapterName, chapterNumber, chapterVolume, message, int.Parse(state));
        interactionState.Steps = stepsService.GetEnabledSteps();

        Log.Information(interactionState.ChapterInfo.ToString());

        var result = await handler.ExecuteAsync(CancellationToken);
        result.LogIfFailed();

        return result.IsSuccess
            ? await discordPublisher.SuccessReleaseMessageAsync(CancellationToken)
            : await discordPublisher.ErrorReleaseMessageAsync(result, CancellationToken);
    }
}