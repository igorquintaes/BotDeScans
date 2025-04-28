using BotDeScans.App.Features.Publish.State;
using BotDeScans.App.Features.Publish.Steps;
using Remora.Discord.Interactivity;
using Remora.Results;
using Serilog;
using System.ComponentModel;
namespace BotDeScans.App.Features.Publish.Discord;

public class PublishInteractions(
    PublishMessageService messageService,
    PublishService publishService,
    PublishState publishState,
    StepsService stepsService) : InteractionGroup
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
        publishState.ReleaseInfo = new(driveUrl, chapterName, chapterNumber, chapterVolume, message, int.Parse(state));
        publishState.Steps = publishService.GetEnabledSteps();

        Log.Information(publishState.ReleaseInfo.ToString());

        var result = await stepsService.ExecuteAsync(CancellationToken);

        return result.IsSuccess
            ? await messageService.SuccessReleaseMessageAsync(CancellationToken)
            : await messageService.ErrorReleaseMessageAsync(result, CancellationToken);
    }
}