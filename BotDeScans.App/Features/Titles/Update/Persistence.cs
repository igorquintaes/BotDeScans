using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotDeScans.App.Features.Titles.Update;

public class Persistence(DatabaseContext databaseContext) : SaveChanges(databaseContext)
{
    private readonly DatabaseContext context = databaseContext;

    public virtual Task<Title?> GetTitleAsync(int id, CancellationToken cancellationToken)
        => context.Titles
            .Include(x => x.References)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
}
