using FluentResults;
using MangaDexSharp;

namespace BotDeScans.App.Extensions;

public static class MangaDexResponseExtensions
{
    public static Result AsResult(this MangaDexRoot response, params int[] ignoreErrorStatus) => 
        Result.Ok().WithErrors(GetErrors(response, ignoreErrorStatus));

    public static Result<T> AsResult<T>(this MangaDexRoot<T> response, params int[] ignoreErrorStatus)
        where T : new() => 
        Result.Ok(response.Data).WithErrors(GetErrors(response, ignoreErrorStatus));

    private static IEnumerable<IError> GetErrors(MangaDexRoot response, params int[] ignoreErrorStatus) => 
        response.Errors
            .Where(x => ignoreErrorStatus.Contains(x.Status) is false)
            .Select(x => new Error($"{x.Status} - {x.Title} - {x.Detail}"));
}
