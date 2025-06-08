using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;
using Microsoft.EntityFrameworkCore;
namespace BotDeScans.App.Features.Publish;

public class PublishQueries(DatabaseContext databaseContext)
{
    public virtual Task<Title> GetTitleAsync(int id, CancellationToken cancellationToken)
        => databaseContext.Titles
            .Include(x => x.References)
            .SingleAsync(x => x.Id == id, cancellationToken);
}
