using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Infra;
using Microsoft.EntityFrameworkCore;

namespace BotDeScans.App.Features.Titles.SkipSteps.List;

public class Persistence(DatabaseContext context)
{
    private readonly DatabaseContext context = context;

    public virtual Task<List<StepName>> GetReferencesAsync(int titleId, CancellationToken cancellationToken) =>
        context.Titles
            .Include(x => x.SkipSteps)
            .Where(x => x.Id == titleId)
            .SelectMany(x => x.SkipSteps)
            .Select(x => x.Step)
            .ToListAsync(cancellationToken);
}
