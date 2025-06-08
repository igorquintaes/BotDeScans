using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotDeScans.App.Features.References.Update;

public class Persistence(DatabaseContext context) : SaveChanges(context)
{
    private readonly DatabaseContext context = context;

    public virtual Task<Title> GetTitleAsync(int id, CancellationToken cancellationToken) =>
        context.Titles
            .Include(x => x.References)
            .Where(x => x.Id == id)
            .SingleAsync(cancellationToken);
}
