using BotDeScans.App.Infra.Repositories;
using FluentResults;

namespace BotDeScans.App.Features.References.List;

public class Handler(TitleRepository titleRepository)
{
    public virtual async Task<Result<string[]>> ExecuteAsync(int titleId, CancellationToken cancellationToken)
    {
        var title = await titleRepository.GetTitleAsync(titleId, cancellationToken);
        if (title is null)
            return Result.Fail("Obra não encontrada.");

        return title.References.Count == 0
            ? ["A obra não contém referências."]
            : title.References
                   .Select((x, index) => new { Number = index + 1, x.Key, x.Value })
                   .Select(x => string.Format("{0}. {1}{2}{3}{2}", x.Number, x.Key.ToString(), Environment.NewLine, x.Value))
                   .ToArray();
    }
}
