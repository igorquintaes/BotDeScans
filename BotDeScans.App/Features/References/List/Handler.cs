using BotDeScans.App.Infra;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace BotDeScans.App.Features.References.List;

public class Handler(DatabaseContext context)
{
    public async Task<Result<string[]>> ExecuteAsync(string titleName, CancellationToken cancellationToken)
    {
        // todo: pagination based on titles length (characters quantity) - discord api
        // low priority due single reference atm
        var title = await context.Titles
            .Where(x => x.Name == titleName)
            .Include(x => x.References)
            .SingleAsync(cancellationToken);

        if (title.References.Count == 0)
            return Result.Fail("Não há referências cadastradas para a obra.");

        var titlesListAsText = title.References
            .Select((x, index) => new { Number = index + 1, x.Key, x.Value })
            .Select(x => string.Format("{0}. {1}{2}{3}{2}", x.Number, x.Key.ToString(), Environment.NewLine, x.Value))
            .ToArray();

        return titlesListAsText;
    }
}
