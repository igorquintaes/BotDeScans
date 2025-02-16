using Remora.Discord.Commands.Contexts;
using Remora.Discord.Interactivity;
using Remora.Results;
using System.ComponentModel;
using static BotDeScans.App.Features.Publish.PublishState;
namespace BotDeScans.App.Features.Publish.Discord;

public class PublishInteractions(
    IOperationContext context,
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
        if (context is not InteractionContext interactionContext)
            return Result.FromSuccess();

        var publishInfo = new Info(driveUrl, chapterName, chapterNumber, chapterVolume, message, int.Parse(state));
        var result = await publishHandler.HandleAsync(publishInfo, interactionContext, CancellationToken);

        return result.IsSuccess
            ? await messageService.PublishReleaseMessageAsync(
                interactionContext: interactionContext,
                content: result.Value,
                CancellationToken)
            : await messageService.PublishErrorReleaseMessageAsync(
                interactionContext: interactionContext,
                errorResult: result.ToResult(),
                CancellationToken);
    }
}