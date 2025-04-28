using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Pings;
using BotDeScans.App.Features.Publish.State;
using BotDeScans.App.Features.Publish.Steps.Enums;
using FluentResults;
using FluentValidation;
namespace BotDeScans.App.Features.Publish.Steps;

public class SetupStep(
    PublishState publishState,
    PublishQueries publishQueries,
    IEnumerable<Ping> pings,
    IValidator<PublishState> publishStateValidator) : IManagementStep
{
    public StepType Type => StepType.Management;
    public StepName Name => StepName.Setup;

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        publishState.Title = await publishQueries.GetTitle(publishState.ReleaseInfo.TitleId, cancellationToken);

        var initialValidation = await publishStateValidator.ValidateAsync(publishState, cancellationToken);
        if (initialValidation.IsValid is false)
            return initialValidation.ToResult();

        var ping = pings.Single(x => x.IsApplicable);
        publishState.InternalData.Pings = await ping.GetPingAsTextAsync(cancellationToken);

        return Result.Ok();
    }
}
