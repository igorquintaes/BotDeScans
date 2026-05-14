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
    State state,
    StepsService stepsService,
    TitleRepository titleRepository,
    IEnumerable<Ping> pings,
    IValidator<State> stateValidator)
{
    public virtual async Task<Result> SetupAsync(Info chapterInfo, CancellationToken cancellationToken)
    {
        state.ChapterInfo = chapterInfo;
        Log.Information(state.ChapterInfo.ToString());

        var title = await titleRepository.GetTitleAsync(chapterInfo.TitleId, cancellationToken);
        if (title is null)
            return Result.Fail("Obra não encontrada.");

        state.Title = title;
        state.Steps = stepsService.GetEnabledSteps(
            [.. title.SkipSteps.Select(s => s.Step)]);

        var initialValidation = await stateValidator.ValidateAsync(state, cancellationToken);
        if (initialValidation.IsValid is false)
            return initialValidation.ToResult();

        var ping = pings.Single(x => x.IsApplicable);
        var pingAsText = await ping.GetPingAsTextAsync(cancellationToken);
        state.SetPings(pingAsText);

        return Result.Ok();
    }
}
