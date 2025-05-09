using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotDeScans.App.Features.References.List;

public class Persistence(DatabaseContext context)
{
    private readonly DatabaseContext context = context;

    public virtual Task<List<TitleReference>> GetReferencesAsync(string titleName, CancellationToken cancellationToken) =>
        context.Titles
            .Where(x => x.Name == titleName)
            .Include(x => x.References)
            .SelectMany(x => x.References)
            .ToListAsync(cancellationToken);
}
