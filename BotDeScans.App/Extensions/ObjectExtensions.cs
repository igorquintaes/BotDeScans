using FluentResults;
using System.Linq.Expressions;

namespace BotDeScans.App.Extensions;

public static class ObjectExtensions
{
    private const string ERROR_MESSAGE = "Fatal error occurred. More information inside log file.";

    public static async Task<Result> SafeCallAsync<TObject>(
        this TObject obj,
        Expression<Func<TObject, Task<Result>>> expression) =>
            await Result.Try(
                action: () => expression.Compile()(obj),
                catchHandler: ex => new Error(ERROR_MESSAGE).CausedBy(ex));

    public static async Task<Result<T>> SafeCallAsync<TObject, T>(
        this TObject obj,
        Expression<Func<TObject, Task<Result<T>>>> expression) =>
            await Result.Try(
                action: () => expression.Compile()(obj),
                catchHandler: ex => new Error(ERROR_MESSAGE).CausedBy(ex));
}