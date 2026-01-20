using FluentResults;
using Serilog;
using System.Linq.Expressions;

namespace BotDeScans.App.Extensions;

public static class ObjectExtensions
{
    public static async Task<Result> SafeCallAsync<TObject>(this TObject obj, Expression<Func<TObject, Task<Result>>> expression)
    {
        var executionResult = await Result.Try(
            () => expression.Compile().Invoke(obj),
            ex =>
            {
                const string ERROR_MESSAGE = "Fatal error ocurred. More information inside log file.";
                Log.Error(ex, ERROR_MESSAGE);
                return new Error(ERROR_MESSAGE).CausedBy(ex);
            });

        return executionResult.IsSuccess 
            ? executionResult.Value 
            : executionResult.ToResult();
    }
}
