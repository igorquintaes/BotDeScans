using BotDeScans.App.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotDeScans.App.Infra.Repositories;

public class TitleRepository(DatabaseContext context) : SaveChanges(context)
{
    private readonly DatabaseContext context = context;

    public virtual Task<Title?> GetTitleAsync(int titleId, CancellationToken cancellationToken) =>
        context.Titles
               .Include(x => x.References)
               .Include(x => x.SkipSteps)
               .FirstOrDefaultAsync(x => x.Id == titleId, cancellationToken);

    public virtual Task<List<Title>> GetTitlesAsync(CancellationToken cancellationToken) =>
        context.Titles
               .Include(x => x.References)
               .Include(x => x.SkipSteps)
               .ToListAsync(cancellationToken);
}
