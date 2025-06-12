namespace BotDeScans.App.Features.References.List;

public class Handler(Persistence persistence)
{
    public virtual async Task<string[]> ExecuteAsync(int titleId, CancellationToken cancellationToken)
    {
        var references = await persistence.GetReferencesAsync(titleId, cancellationToken);

        return references.Count == 0
            ? ["A obra não contém referências."]
            : references.Select((x, index) => new { Number = index + 1, x.Key, x.Value })
                        .Select(x => string.Format("{0}. {1}{2}{3}{2}", x.Number, x.Key.ToString(), Environment.NewLine, x.Value))
                        .ToArray();
    }
}
