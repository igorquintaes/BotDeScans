using BotDeScans.App.Infra;
using BotDeScans.App.Models;
using Microsoft.EntityFrameworkCore;
namespace BotDeScans.App.Features.Publish;

public class PublishQueries(DatabaseContext databaseContext)
{
    public virtual Task<Title?> GetTitle(int titleId, CancellationToken cancellationToken) 
        => databaseContext.Titles
            .Include(x => x.References)
            .FirstOrDefaultAsync(x => x.Id == titleId, cancellationToken);
}
