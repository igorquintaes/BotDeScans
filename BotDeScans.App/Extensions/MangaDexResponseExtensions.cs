using FluentResults;
using MangaDexSharp;

namespace BotDeScans.App.Extensions;

public static class MangaDexResponseExtensions
{
    public static Result AsResult(this MangaDexRoot mangaDexResponse, params int[] allowedStatusCodes)
    {
        var errors = GetErrors(mangaDexResponse, allowedStatusCodes);

        return Result.Ok().WithErrors(errors);
    }

    public static Result<T> AsResult<T>(this MangaDexRoot<T> mangaDexResponse, params int[] allowedStatusCodes) 
        where T : new()
    {
        var errors = GetErrors(mangaDexResponse, allowedStatusCodes);

        return Result.Ok(mangaDexResponse.Data).WithErrors(errors);
    }

    private static IEnumerable<IError> GetErrors(MangaDexRoot mangaDexResponse, params int[] allowedStatusCodes)
    {
        var mangaDexErrors = mangaDexResponse.Errors.Where(x => allowedStatusCodes.Contains(x.Status) is false);
        return mangaDexErrors.Select(x => new Error($"{x.Status} - {x.Title} - {x.Detail}"));
    }
}
