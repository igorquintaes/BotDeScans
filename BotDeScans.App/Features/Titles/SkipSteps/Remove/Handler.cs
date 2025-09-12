using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities.Enums;

namespace BotDeScans.App.Features.Titles.SkipSteps.Remove;

public class Handler(
    Persistence persistence,
    DatabaseContext databaseContext)
{
    public virtual async Task ExecuteAsync(
        int titleId,
        StepName stepName,
        CancellationToken cancellationToken)
    {
        var title = await persistence.GetTitleAsync(titleId, cancellationToken);
        title.RemoveSkipStep(stepName);

        await databaseContext.SaveChangesAsync(cancellationToken);
    }
}
