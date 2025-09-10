using BotDeScans.App.Extensions;

namespace BotDeScans.App.Features.Titles.SkipSteps.List;

public class Handler(Persistence persistence)
{
    public virtual async Task<string[]> ExecuteAsync(int titleId, CancellationToken cancellationToken)
    {
        var references = await persistence.GetReferencesAsync(titleId, cancellationToken);

        return references.Count == 0
            ? ["A obra não contém procedimentos de publicação a serem ignorados."]
            : references.Select((x, index) => $"{index + 1}. {x.GetDescription()}")
                        .ToArray();
    }
}
