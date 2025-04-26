using Remora.Discord.Interactivity;
using Remora.Results;
using System.ComponentModel;
using static BotDeScans.App.Features.Publish.PublishState;
namespace BotDeScans.App.Features.Publish.Discord;

public class PublishInteractions(
    PublishHandler publishHandler,
    PublishMessageService messageService) : InteractionGroup
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
        var publishInfo = new Info(driveUrl, chapterName, chapterNumber, chapterVolume, message, int.Parse(state));
        var result = await publishHandler.HandleAsync(publishInfo, CancellationToken);

        // todo: publicar no discord deve ser um novo step, eliminando lógica daqui.
        return result.IsSuccess
            ? await messageService.SuccessReleaseMessageAsync(
                content: result.Value,
                CancellationToken)
            : await messageService.ErrorReleaseMessageAsync(
                errorResult: result.ToResult(),
                CancellationToken);
    }
}