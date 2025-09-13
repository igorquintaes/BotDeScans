using BotDeScans.App.Infra;
using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models.Entities.Enums;
using FluentResults;

namespace BotDeScans.App.Features.Titles.SkipSteps.Add;

public class Handler(
    TitleRepository titleRepository,
    DatabaseContext databaseContext)
{
    public virtual async Task<Result> ExecuteAsync(
        int titleId,
        StepName stepName,
        CancellationToken cancellationToken)
    {
        var title = await titleRepository.GetTitleAsync(titleId, cancellationToken);
        if (title is null)
            return Result.Fail("Obra não encontrada.");

        title.AddSkipStep(stepName);

        await databaseContext.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
