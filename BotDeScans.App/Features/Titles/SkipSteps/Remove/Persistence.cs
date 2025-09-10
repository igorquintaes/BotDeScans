using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotDeScans.App.Features.Titles.SkipSteps.Remove;

public class Persistence(DatabaseContext context)
{
    private readonly DatabaseContext context = context;

    public virtual Task<Title> GetTitle(int titleId, CancellationToken cancellationToken) =>
        context.Titles
            .Include(x => x.SkipSteps)
            .Where(x => x.Id == titleId)
            .SingleAsync(cancellationToken);
}
