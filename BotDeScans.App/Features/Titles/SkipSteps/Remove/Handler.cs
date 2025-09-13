using BotDeScans.App.Infra;
using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models.Entities.Enums;
using FluentResults;

namespace BotDeScans.App.Features.Titles.SkipSteps.Remove;

public class Handler(
    TitleRepository titleResult,
    DatabaseContext databaseContext)
{
    public virtual async Task<Result> ExecuteAsync(
        int titleId,
        StepName stepName,
        CancellationToken cancellationToken)
    {
        var title = await titleResult.GetTitleAsync(titleId, cancellationToken);
        if (title is null)
            return Result.Fail("Obra não encontrada.");

        title.RemoveSkipStep(stepName);

        await databaseContext.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
