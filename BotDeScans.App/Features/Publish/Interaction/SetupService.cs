using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Pings;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models.DTOs;
using FluentResults;
using FluentValidation;
using Serilog;

namespace BotDeScans.App.Features.Publish.Interaction;

public class SetupService(
    StepsService stepsService,
    TitleRepository titleRepository,
    IEnumerable<Ping> pings,
    IValidator<State> stateValidator)
{
    public virtual async Task<Result<State>> SetupAsync(Info chapterInfo, CancellationToken cancellationToken)
    {
        Log.Information(chapterInfo.ToString());

        var title = await titleRepository.GetTitleAsync(chapterInfo.TitleId, cancellationToken);
        if (title is null)
            return Result.Fail("Obra não encontrada.");

        var steps = stepsService.GetEnabledSteps(
            [.. title.SkipSteps.Select(s => s.Step)]);

        var state = new State
        {
            ChapterInfo = chapterInfo,
            Title = title,
            Steps = steps
        };

        var initialValidation = await stateValidator.ValidateAsync(state, cancellationToken);
        if (initialValidation.IsValid is false)
            return initialValidation.ToResult();

        var ping = pings.Single(x => x.IsApplicable);
        var pingAsText = await ping.GetPingAsTextAsync(cancellationToken);
        state = state.WithPings(pingAsText);

        return Result.Ok(state);
    }
}
