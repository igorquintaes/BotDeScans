using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotDeScans.App.Features.Titles.SkipSteps.Add;

public class Persistence(DatabaseContext context)
{
    private readonly DatabaseContext context = context;

    public virtual Task<Title> GetTitleAsync(int titleId, CancellationToken cancellationToken) =>
        context.Titles
            .Include(x => x.SkipSteps)
            .Where(x => x.Id == titleId)
            .SingleAsync(cancellationToken);
}
