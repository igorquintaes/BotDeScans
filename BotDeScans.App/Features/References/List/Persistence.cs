using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotDeScans.App.Features.References.List;

public class Persistence(DatabaseContext context)
{
    private readonly DatabaseContext context = context;

    public virtual Task<List<TitleReference>> GetReferencesAsync(int titleId, CancellationToken cancellationToken) =>
        context.Titles
            .Include(x => x.References)
            .Where(x => x.Id == titleId)
            .SelectMany(x => x.References)
            .ToListAsync(cancellationToken);
}
