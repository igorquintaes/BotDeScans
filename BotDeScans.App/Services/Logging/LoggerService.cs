using FluentResults;
using Remora.Results;
using Serilog;
using System.Text;
namespace BotDeScans.App.Services.Logging;

public class LoggerService
{
    public void LogErrors(
        string errorTitle,
        IEnumerable<IError> errors,
        StringBuilder? stringBuilder = null,
        bool finalize = true)
    {
        stringBuilder ??= new StringBuilder();
        stringBuilder.AppendLine(errorTitle);
        stringBuilder.AppendLine("Details: ");
        foreach (var error in errors)
        {
            stringBuilder.AppendLine(string.Empty);
            stringBuilder.AppendLine(error.Message);

            if (error is ExceptionalError exceptionalError)
            {
                stringBuilder.AppendLine("Exception details:");
                stringBuilder.AppendLine(exceptionalError.Exception.ToString());
            }

            if (error.Reasons.Count != 0)
                LogErrors("Inner errors:", error.Reasons, stringBuilder, false);
        }

        if (finalize)
        {
            var stringLog = stringBuilder.ToString();
            Console.Error.WriteLine(stringLog);
            Log.Fatal(stringLog);

            Console.ReadKey();
            throw new InvalidOperationException(errorTitle);
        }
    }

    public void LogErrors(
        string errorTitle,
        IResult? result,
        StringBuilder? stringBuilder = null,
        bool finalize = true)
    {
        if (result is null || result.IsSuccess is true)
            return;

        stringBuilder ??= new StringBuilder();
        stringBuilder.AppendLine(errorTitle);
        stringBuilder.AppendLine("Details: ");

        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine(result.Error.Message);
        LogErrors("Inner errors:", result.Inner, stringBuilder, false);

        if (finalize)
        {
            var stringLog = stringBuilder.ToString();
            Console.Error.WriteLine(stringLog);
            Log.Fatal(stringLog);

            throw new InvalidOperationException(errorTitle);
        }
    }
}