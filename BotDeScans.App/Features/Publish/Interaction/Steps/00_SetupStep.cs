using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Pings;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models.Entities.Enums;
using FluentResults;
using FluentValidation;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class SetupStep(
    State state,
    TitleRepository titleRepository,
    IEnumerable<Ping> pings,
    IValidator<State> stateValidator) : IManagementStep
{
    public StepType Type => StepType.Management;
    public StepName Name => StepName.Setup;
    public bool IsMandatory => true;

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var title = await titleRepository.GetTitleAsync(state.ChapterInfo.TitleId, cancellationToken);
        if (title is null)
            return Result.Fail("Obra não encontrada.");

        state.Title = title;

        var initialValidation = await stateValidator.ValidateAsync(state, cancellationToken);
        if (initialValidation.IsValid is false)
            return initialValidation.ToResult();

        var ping = pings.Single(x => x.IsApplicable);
        state.InternalData.Pings = await ping.GetPingAsTextAsync(cancellationToken);

        return Result.Ok();
    }
}
