using FluentResults;
using Microsoft.Extensions.Logging;
using ILogger = Serilog.ILogger;

namespace BotDeScans.App.Services.Logging;

public class ResultLogger(ILogger logger) : IResultLogger
{
    public void Log(string context, string content, ResultBase result, LogLevel level)
    {
        var serilogLevel = level switch
        {
            LogLevel.Information => Serilog.Events.LogEventLevel.Information,
            LogLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            LogLevel.Error => Serilog.Events.LogEventLevel.Error,
            LogLevel.Critical => Serilog.Events.LogEventLevel.Fatal,
            _ => Serilog.Events.LogEventLevel.Verbose
        };

        logger.Write(serilogLevel, "FluentResults [{Context}]: {Content}", context, content);
    }

    public void Log<TContext>(string content, ResultBase result, LogLevel level) =>
        Log(typeof(TContext).Name, content, result, level);
}
