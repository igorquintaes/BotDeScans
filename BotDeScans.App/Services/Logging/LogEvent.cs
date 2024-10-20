using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Results;
using Serilog;
namespace BotDeScans.App.Services.Logging;

public class LogEvent(ILogger logger) : IPostExecutionEvent
{
    private readonly ILogger logger = logger;

    public Task<Result> AfterExecutionAsync(
        ICommandContext context,
        IResult commandResult,
        CancellationToken ct = default)
    {
        LogErrors(commandResult);
        return Task.FromResult(Result.FromSuccess());
    }

    private void LogErrors(IResult commandResult)
    {
        if (!commandResult.IsSuccess)
        {
            logger.Error(commandResult.Error.Message);
            if (commandResult.Inner != null)
                LogErrors(commandResult.Inner);
        }
    }
}
