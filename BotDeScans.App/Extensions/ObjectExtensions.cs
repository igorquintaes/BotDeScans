using FluentResults;
using Serilog;
using System.Linq.Expressions;

namespace BotDeScans.App.Extensions;

public static class ObjectExtensions
{
    public static async Task<Result> SafeCallAsync<TObject>(this TObject obj, Expression<Func<TObject, Task<Result>>> expression) =>
        await Result.Try(
            action: () => expression.Compile().Invoke(obj),
            catchHandler: ex =>
            {
                const string ERROR_MESSAGE = "Fatal error occurred. More information inside log file.";
                Log.Error(ex, ERROR_MESSAGE);
                return new Error(ERROR_MESSAGE).CausedBy(ex);
            });
}
