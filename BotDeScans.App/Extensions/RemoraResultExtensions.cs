using FluentResults;

namespace BotDeScans.App.Extensions;

public static class RemoraResultExtensions
{
    private const string DEFAULT_ERROR = "Error in Discord communication.";

    public static Result<T> ToFluentResult<T>(this Remora.Results.IResult result, Func<T> valueFactory) =>
        result.IsSuccess
             ? valueFactory()
             : Result.Fail([DEFAULT_ERROR, result.Error.Message]);
}
