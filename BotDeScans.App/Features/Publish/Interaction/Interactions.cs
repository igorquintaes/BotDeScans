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
        var info = new Info(driveUrl, chapterName, chapterNumber, chapterVolume, message, int.Parse(state));
        
        var setupResult = await setupService.SetupAsync(info, CancellationToken);
        if (setupResult.IsFailed)
            return await discordPublisher.ErrorReleaseMessageAsync(setupResult.ToResult(), CancellationToken);

        var executeResult = await handler.ExecuteAsync(setupResult.Value, CancellationToken);

        return executeResult.IsSuccess
            ? await discordPublisher.SuccessReleaseMessageAsync(executeResult.Value, CancellationToken)
            : await discordPublisher.ErrorReleaseMessageAsync(executeResult.ToResult(), CancellationToken);
    }
}