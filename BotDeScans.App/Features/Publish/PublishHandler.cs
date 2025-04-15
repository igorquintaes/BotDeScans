using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.Pings;
using BotDeScans.App.Features.Publish.Steps.Enums;
using FluentResults;
using FluentValidation;
using Remora.Discord.Commands.Contexts;
using Serilog;
using static BotDeScans.App.Features.Publish.PublishState;
namespace BotDeScans.App.Features.Publish;

public class PublishHandler(
    PublishState publishState,
    PublishService publishService,
    PublishMessageService publishMessageService,
    PublishQueries publishQueries,
    IEnumerable<Ping> pings,
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

        var ping = pings.Single(x => x.IsApplicable);
        var pingResult = await ping.GetPingAsTextAsync(cancellationToken);
        if (pingResult.IsFailed)
            return pingResult;

        var initialFeedbackResult = await publishMessageService.UpdateTrackingMessageAsync(interactionContext, cancellationToken);
        if (initialFeedbackResult.IsFailed)
            return initialFeedbackResult;

        var preValidationResult = await publishService.ValidateBeforeFilesManagementAsync(interactionContext, cancellationToken);
        if (preValidationResult.IsFailed)
            return preValidationResult;

        var managementResult = await publishService.ExecuteStepsAsync(interactionContext, StepType.Management, cancellationToken);
        if (managementResult.IsFailed)
            return managementResult;

        var validationResult = await publishService.ValidateAfterFilesManagementAsync(interactionContext, cancellationToken);
        if (validationResult.IsFailed)
            return validationResult;

        var uploadResult = await publishService.ExecuteStepsAsync(interactionContext, StepType.Upload, cancellationToken);
        if (uploadResult.IsFailed)
            return uploadResult;

        var publishResult = await publishService.ExecuteStepsAsync(interactionContext, StepType.Publish, cancellationToken);
        if (publishResult.IsFailed)
            return publishResult;

        return pingResult;
    }
}
