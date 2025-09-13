using BotDeScans.App.Extensions;
using BotDeScans.App.Infra.Repositories;
using FluentResults;

namespace BotDeScans.App.Features.Titles.SkipSteps.List;

public class Handler(TitleRepository titleRepository)
{
    public virtual async Task<Result<string[]>> ExecuteAsync(int titleId, CancellationToken cancellationToken)
    {
        var title = await titleRepository.GetTitleAsync(titleId, cancellationToken);
        if (title is null)
            return Result.Fail("Obra não encontrada.");

        return title.SkipSteps.Count == 0
            ? ["A obra não contém procedimentos de publicação a serem ignorados."]
            : title.SkipSteps.Select((x, index) => $"{index + 1}. {x.Step.GetDescription()}")
                             .ToArray();
    }
}
