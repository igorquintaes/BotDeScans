using BotDeScans.App.Infra;
using BotDeScans.App.Models;
using Microsoft.EntityFrameworkCore;
namespace BotDeScans.App.Features.Publish;

public class PublishQueries(DatabaseContext databaseContext)
{
    public virtual Task<Title> GetTitle(int titleId, CancellationToken cancellationToken)
        => databaseContext.Titles
            .Include(x => x.References)
            .SingleAsync(x => x.Id == titleId, cancellationToken);

    public virtual Task<int> GetTitleId(string titleName, CancellationToken cancellationToken) =>
        databaseContext.Titles
            .Where(x => x.Name == titleName)
            .Select(x => x.Id)
            .SingleAsync(cancellationToken);
}
