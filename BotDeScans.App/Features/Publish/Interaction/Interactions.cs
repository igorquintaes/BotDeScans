using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Models.DTOs;
using Remora.Discord.Interactivity;
using Remora.Results;
using System.ComponentModel;

namespace BotDeScans.App.Features.Publish.Interaction;

public class Interactions(
    DiscordPublisher discordPublisher,
    SetupService setupService,
    Handler handler) : InteractionGroup
{
    public const string MODAL_NAME = "Features.Publish";

    [Modal(MODAL_NAME)]
    [Description("Publica novo lançamento")]
    public async Task<IResult> ExecuteAsync(
        string driveUrl,
        string chapterName,
        string chapterNumber,
        string chapterVolume,
        string message,
        string state)
    {
        var setupResult = await setupService.SetupAsync(
            new Info(driveUrl, chapterName, chapterNumber, chapterVolume, message, int.Parse(state)),
            CancellationToken);
        setupResult.LogIfFailed();
        if (setupResult.IsFailed)
            return await discordPublisher.ErrorReleaseMessageAsync(setupResult, CancellationToken);

        var result = await handler.ExecuteAsync(CancellationToken);
        result.LogIfFailed();

        return result.IsSuccess
            ? await discordPublisher.SuccessReleaseMessageAsync(CancellationToken)
            : await discordPublisher.ErrorReleaseMessageAsync(result, CancellationToken);
    }
}