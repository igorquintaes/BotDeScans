using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotDeScans.App.Features.Titles.Update;

public class Persistence(DatabaseContext context) : SaveChanges(context)
{
    private readonly DatabaseContext context = context;

    public virtual Task<Title?> GetTitleAsync(int id, CancellationToken cancellationToken)
        => context.Titles
            .Include(x => x.References)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
}
