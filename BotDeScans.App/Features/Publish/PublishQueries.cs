using BotDeScans.App.Infra;
using BotDeScans.App.Models;
using Microsoft.EntityFrameworkCore;
namespace BotDeScans.App.Features.Publish;

public class PublishQueries(DatabaseContext databaseContext)
{
    public virtual Task<Title> GetTitleAsync(int id, CancellationToken cancellationToken)
        => databaseContext.Titles
            .Include(x => x.References)
            .SingleAsync(x => x.Id == id, cancellationToken);

    public virtual Task<int> GetTitleIdAsync(string name, CancellationToken cancellationToken) =>
        databaseContext.Titles
            .Where(x => x.Name == name)
            .Select(x => x.Id)
            .SingleAsync(cancellationToken);
}
