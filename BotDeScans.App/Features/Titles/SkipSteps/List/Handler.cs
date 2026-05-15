using BotDeScans.App.Extensions;
using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models.Entities;
using FluentResults;

namespace BotDeScans.App.Features.Titles.SkipSteps.List;

public class Handler(TitleRepository titleRepository)
{
    private const string NOT_FOUND_ERROR = "Obra não encontrada.";
    private const string NO_STEPS_MESSAGE = "A obra não contém procedimentos de publicação a serem ignorados.";

    public virtual async Task<Result<string[]>> ExecuteAsync(int titleId, CancellationToken cancellationToken)
    {
        var title = await titleRepository.GetTitleAsync(titleId, cancellationToken);

        return title is not null
             ? title.SkipSteps
                 .Select(GetLine)
                 .DefaultIfEmpty(NO_STEPS_MESSAGE)
                 .ToArray()
             : Result.Fail(NOT_FOUND_ERROR);

        static string GetLine(SkipStep x, int index) => 
            $"{index + 1}. {x.Step.GetDescription()}";
    }
}
