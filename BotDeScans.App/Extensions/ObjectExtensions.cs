using FluentResults;
using Serilog;
using System.Linq.Expressions;

namespace BotDeScans.App.Extensions;

public static class ObjectExtensions
{
    private const string ERROR_MESSAGE = "Fatal error occurred. More information inside log file.";

    public static async Task<Result> SafeCallAsync<TObject>(
        this TObject obj,
        Expression<Func<TObject, Task<Result>>> expression)
    {
        var result = await Result.Try(
                action: () => expression.Compile()(obj),
                catchHandler: ex => new Error(ERROR_MESSAGE).CausedBy(ex));

        LogFailure(result);
        return result;
    }

    public static async Task<Result<T>> SafeCallAsync<TObject, T>(
        this TObject obj,
        Expression<Func<TObject, Task<Result<T>>>> expression)
    {
        var result = await Result.Try(
                action: () => expression.Compile()(obj),
                catchHandler: ex => new Error(ERROR_MESSAGE).CausedBy(ex));

        LogFailure(result);
        return result;
    }

    private static void LogFailure(ResultBase result)
    {
        foreach (var exception in result.Errors
                     .SelectMany(error => error.Reasons)
                     .OfType<ExceptionalError>()
                     .Select(error => error.Exception))
            Log.Error(exception, "Unhandled exception captured while executing an operation.");
    }
}