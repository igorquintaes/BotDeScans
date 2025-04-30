using FluentResults;
using Serilog;
using System.Linq.Expressions;
namespace BotDeScans.App.Extensions;

public static class ObjectExtensions
{
    public static async Task<Result> SafeCallAsync<TObject>(this TObject obj, Expression<Func<TObject, Task<Result>>> expression)
    {
        try { return await expression.Compile().Invoke(obj); }
        catch (Exception ex)
        {
            const string ERROR_MESSAGE = "Fatal error ocurred. More information inside log file.";
            Log.Error(ex, ERROR_MESSAGE);

            return Result.Fail(new Error(ERROR_MESSAGE).CausedBy(ex));
        }
    }
}
