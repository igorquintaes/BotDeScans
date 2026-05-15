using FluentResults;
using Google;
using System.Text.Json;

namespace BotDeScans.App.Extensions;

public static class FluentResultsExtensions
{
    public static string ToValidationErrorMessage(this ResultBase result)
    {
        // Fluent validation throws an exception due null or empty error message before validation execution.
        // We need a default valid string here because we get error message in runtime.
        // The string below is not going to be used.
        if (result.IsSuccess)
            return "Success.";

        var errorMessages = result.Errors.GetErrorsInfo().Select(x => x.Message);
        return string.Join("; ", errorMessages);
    }

    public static Remora.Results.Result ToDiscordResult(this ResultBase result)
    {
        if (result.IsSuccess)
            return Remora.Results.Result.FromSuccess();

        var errorsInfo = result.Errors.GetErrorsInfo();
        var errorsInfoAsString = JsonSerializer.Serialize(errorsInfo);
        var remoraError = new Remora.Results.InvalidOperationError(errorsInfoAsString);
        return Remora.Results.Result.FromError(remoraError);
    }

    public static Result FailIf(this Result result, Func<bool> condition, string message) =>
        condition.Invoke()
            ? result.WithError(message)
            : result;

    public static IEnumerable<ErrorInfo> GetErrorsInfo(this IReadOnlyList<IError> errors, int depth = 0)
    {
        for (var index = 0; index < errors.Count; index++)
        {
            var error = errors[index];
            var errorNumber = index + 1;

            yield return TryGetExceptionMessageFromError(error, out var exceptionMessage)
                ? new(exceptionMessage, errorNumber, depth, ErrorType.Exception)
                : new(error.Message, errorNumber, depth, ErrorType.Regular);

            foreach (var innerError in GetErrorsInfo(error.Reasons, depth + 1))
                yield return innerError;
        }

        static bool TryGetExceptionMessageFromError(IError error, out string exceptionMessage)
        {
            exceptionMessage = string.Empty;
            if (error is ExceptionalError exceptionalError)
            {
                if (exceptionalError.Exception is GoogleApiException googleException)
                {
                    var code = googleException.HttpStatusCode;
                    var intCode = (int)code;
                    exceptionMessage = $"O Google retornou o HTTP Status Code [{code} ({intCode})](https://http.cat/{intCode})";
                }
                else
                    exceptionMessage = exceptionalError.Exception.Message;

                return true;
            }

            return false;
        }
    }
}

public record ErrorInfo(string Message, int Number, int Depth, ErrorType Type);

public enum ErrorType { Regular, Exception }
