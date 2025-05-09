using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Infra;

[ExcludeFromCodeCoverage(Justification = "Save wrapper")]
public abstract class SaveChanges(DatabaseContext context)
{
    public virtual Task<int> SaveAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
