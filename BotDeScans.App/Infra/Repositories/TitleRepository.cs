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

    public async virtual Task<IReadOnlyList<Title>> GetTitlesAsync(CancellationToken cancellationToken) =>
        await context.Titles
            .Include(x => x.References)
            .Include(x => x.SkipSteps)
            .ToListAsync(cancellationToken);

    public virtual async Task<IReadOnlyList<Title>> GetTitlesByNameLikeAsync(string nameLike, int resultsQuantity, CancellationToken cancellationToken) =>
        await context.Titles
            .Where(x => EF.Functions.Like(x.Name, $"%{nameLike}%"))
            .OrderBy(x => x.Name)
            .Take(resultsQuantity)
            .ToListAsync(cancellationToken);
}
