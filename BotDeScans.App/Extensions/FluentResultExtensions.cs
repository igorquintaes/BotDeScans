using BotDeScans.App.Builders;
using BotDeScans.App.Models;
using BotDeScans.App.Services.Discord;
using FluentResults;
using FluentValidation.Results;
using Google;
using MangaDexSharp;
using System.Text.Json;

namespace BotDeScans.App.Extensions;

public static class FluentResultExtensions
{
    public static Result WithConditionalError(this Result result, Func<bool> conditionToAddError, string error)
        => conditionToAddError.Invoke()
            ? result.WithError(error)
            : result;

    public static Result AsFailResult<T>(this MangaDexRoot<T> mangaDexResponse) where T : new()
        => Result.Fail(GetErrors(mangaDexResponse.Errors));

    public static Result AsFailResult(this MangaDexRoot mangaDexResponse)
        => Result.Fail(GetErrors(mangaDexResponse.Errors));

    public static Result ToResult(this ValidationResult validationResult)
        => validationResult.IsValid
            ? Result.Ok()
            : Result.Fail(validationResult.Errors.Select(validationError =>
                     new Error(validationError.ErrorMessage)));

    public static IEnumerable<ErrorInfo> GetErrorsInfo(this IReadOnlyList<IError> errors, int depth = 0)
    {
        for (var index = 0; index < errors.Count; index++)
        {
            var error = errors[index];
            var errorNumber = index + 1;
            yield return new(error.Message, errorNumber, depth, ErrorType.Regular);

            if (error.TryGetExceptionMessageFromError(out var exceptionMessage))
                yield return new(exceptionMessage, errorNumber, depth, ErrorType.Exception);

            foreach (var innerError in GetErrorsInfo(error.Reasons, depth + 1))
                yield return innerError;
        }
    }

    public static bool TryGetExceptionMessageFromError(this IError error, out string exceptionMessage)
    {
        exceptionMessage = string.Empty;
        if (error is ExceptionalError exceptionalError)
        {
            if (exceptionalError.Exception is GoogleApiException googleException)
            {
                var code = googleException.HttpStatusCode;
                var intCode = (int)code;
                exceptionMessage = $"O Google retornou o HTTP Status Code[{code} ({intCode})](https://http.cat/{intCode})";
            }
            else
                exceptionMessage = exceptionalError.Exception.Message;

            return true;
        }

        return false;
    }

    public static Remora.Results.InvalidOperationError ToDiscordError(this List<IError> errors)
    {
        var errorsInfo = errors.GetErrorsInfo();
        var errorsInfoAsString = JsonSerializer.Serialize(errorsInfo);
        return new Remora.Results.InvalidOperationError(errorsInfoAsString);
    }

    public static async Task<Remora.Results.IResult> PostErrorOnDiscord(this Result result, ExtendedFeedbackService feedbackService, CancellationToken cancellationToken)
    {
        // todo: tirar daqui. Mesma classe que EmbedBuilder.HandleTasksAndUpdateMessage, que também precisa mover. E usar IoC pro service
        if (result.IsSuccess)
            throw new InvalidOperationException("this method need be called only for failed results."); ;

        var errorEmbed = EmbedBuilder.CreateErrorEmbed(result);
        return await feedbackService.SendContextualEmbedAsync(errorEmbed, ct: cancellationToken);
    }

    public static Task<Remora.Results.IResult> PostErrorOnDiscord<T>(this Result<T> result, ExtendedFeedbackService feedbackService, CancellationToken cancellationToken)
        => result.ToResult().PostErrorOnDiscord(feedbackService, cancellationToken);

    private static IEnumerable<Error> GetErrors(MangaDexError[] mangaDexErrors)
        => mangaDexErrors.Length > 0
            ? mangaDexErrors.Select(x => new Error($"{x.Status} - {x.Title} - {x.Detail}"))
            : ([new Error("Generic error")]);
}
