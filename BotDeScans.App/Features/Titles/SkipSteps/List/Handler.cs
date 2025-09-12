using BotDeScans.App.Extensions;

namespace BotDeScans.App.Features.Titles.SkipSteps.List;

public class Handler(Persistence persistence)
{
    public virtual async Task<string[]> ExecuteAsync(int titleId, CancellationToken cancellationToken)
    {
        var stepNames = await persistence.GetStepNamesAsync(titleId, cancellationToken);

        return stepNames.Count == 0
            ? ["A obra não contém procedimentos de publicação a serem ignorados."]
            : stepNames.Select((x, index) => $"{index + 1}. {x.GetDescription()}")
                       .ToArray();
    }
}
