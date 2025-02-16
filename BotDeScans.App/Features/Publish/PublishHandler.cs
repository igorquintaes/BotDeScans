using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Infra;
using FluentResults;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.Commands.Contexts;
using Serilog;
using static BotDeScans.App.Features.Publish.PublishState;
namespace BotDeScans.App.Features.Publish;

public class PublishHandler(
    PublishState publishState,
    PublishService publishService,
    PublishMessageService publishMessageService,
    PublishQueries publishQueries,
    IValidator<Info> validator)
{
    public async Task<Result<string>> HandleAsync(
        Info info,
        InteractionContext interactionContext,
        CancellationToken cancellationToken)
    {
        var infoValidationResult = validator.Validate(info);
        if (infoValidationResult.IsValid is false)
            return infoValidationResult.ToResult();

        var title = await publishQueries.GetTitle(info.TitleId, cancellationToken);
        if (title is null)
            return Result.Fail("Obra não encontrada.");

        publishState.Title = title;
        publishState.ReleaseInfo = info;
        Log.Information(info.ToString());

        var initialFeedbackResult = await publishMessageService.SendOrEditTrackingMessageAsync(interactionContext, cancellationToken);
        if (initialFeedbackResult.IsFailed)
            return initialFeedbackResult;

        var pingResult = await publishService.CreatePingMessageAsync(cancellationToken);
        if (pingResult.IsFailed)
            return pingResult;

        var preValidationResult = await publishService.ValidateBeforeFilesManagementAsync(interactionContext, cancellationToken);
        if (preValidationResult.IsFailed)
            return preValidationResult;

        var managementResult = await publishService.RunManagementStepsAsync(interactionContext, cancellationToken);
        if (managementResult.IsFailed)
            return managementResult;

        var validationResult = await publishService.ValidateAfterFilesManagementAsync(interactionContext, cancellationToken);
        if (validationResult.IsFailed)
            return validationResult;

        var publishResult = await publishService.RunPublishStepsAsync(interactionContext, cancellationToken);
        if (publishResult.IsFailed)
            return publishResult;

        return pingResult;
    }
}
