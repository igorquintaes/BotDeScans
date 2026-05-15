using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models.Entities;
using FluentResults;

namespace BotDeScans.App.Features.References.List;

public class Handler(TitleRepository titleRepository)
{
    public virtual async Task<Result<string[]>> ExecuteAsync(int titleId, CancellationToken cancellationToken)
    {
        const string NOT_FOUND_ERROR = "Obra não encontrada.";
        const string NO_REFERENCES_MESSAGE = "A obra não contém referências.";

        var title = await titleRepository.GetTitleAsync(titleId, cancellationToken);
        return title is not null
             ? title.References
                    .Select(GetReferences)
                    .DefaultIfEmpty(NO_REFERENCES_MESSAGE)
                    .ToArray()
             : Result.Fail(NOT_FOUND_ERROR);

        static string GetReferences(TitleReference reference, int index) => 
            string.Format("{0}. {1}{2}{3}{2}",
                index + 1,
                reference.Key.ToString(),
                Environment.NewLine,
                reference.Value);
    }
}
