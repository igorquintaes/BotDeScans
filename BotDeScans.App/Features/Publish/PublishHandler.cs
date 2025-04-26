using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Pings;
using BotDeScans.App.Features.Publish.Steps;
using FluentResults;
using FluentValidation;
using Remora.Discord.Commands.Contexts;
using Serilog;
using static BotDeScans.App.Features.Publish.PublishState;
namespace BotDeScans.App.Features.Publish;

public class PublishHandler(
    IOperationContext operationContext,
    StepsService stepsService,
    PublishState publishState,
    PublishQueries publishQueries,
    PublishService publishService,
    IEnumerable<Ping> pings,
    IValidator<PublishState> publishValidator)
{
    public async Task<Result<string>> HandleAsync(Info info, CancellationToken cancellationToken)
    {
        Log.Information(info.ToString());

        var title = await publishQueries.GetTitle(info.TitleId, cancellationToken);
        if (title is null)
            return Result.Fail("Obra não encontrada.");

        var ping = pings.Single(x => x.IsApplicable);
        var pingResult = await ping.GetPingAsTextAsync(cancellationToken);
        if (pingResult.IsFailed)
            return pingResult;

        publishState.Title = title;
        publishState.ReleaseInfo = info;
        publishState.Steps = publishService.GetPublishStepsNames();

        var infoValidationResult = await publishValidator.ValidateAsync(publishState, cancellationToken);
        if (infoValidationResult.IsValid is false)
            return infoValidationResult.ToResult();

        var stepsResult = await stepsService.ExecuteAsync(operationContext, cancellationToken);
        if (stepsResult.IsFailed)
            return stepsResult;

        return pingResult;
    }
}
