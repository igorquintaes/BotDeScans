﻿using BotDeScans.App.Builders;
using BotDeScans.App.Services.Discord;
using FluentResults;
using FluentValidation.Results;
using Google;
using MangaDexSharp;
using System.Text.Json;
namespace BotDeScans.App.Extensions;

public static class FluentResultExtensions
{
    public static string ToValidationErrorMessage(this ResultBase result)
    {
        // Fluent validation throws an exception due null or empty error message.
        // Needs of execution time error messages can lead exeptions here without a default valid string.
        if (result.IsSuccess)
            return "Ignore.";

        var errorMessages = result.Errors.GetErrorsInfo().Select(x => x.Message);
        return string.Join("; ", errorMessages);
    }

    public static Result WithConditionalError(this Result result, Func<bool> conditionToAddError, string error) =>
        conditionToAddError.Invoke()
            ? result.WithError(error)
            : result;

    public static Result<T> AsResult<T>(this MangaDexRoot<T> mangaDexResponse, params int[] allowedStatusCodes) where T : new()
    {
        if (mangaDexResponse.Errors.All(x => allowedStatusCodes.Contains(x.Status)))
            return Result.Ok(mangaDexResponse.Data);

        return Result.Fail(GetErrors(mangaDexResponse));
    }

    public static Result AsResult(this MangaDexRoot mangaDexResponse, params int[] allowedStatusCodes)
    {
        if (mangaDexResponse.Errors.All(x => allowedStatusCodes.Contains(x.Status)))
            return Result.Ok();

        return Result.Fail(GetErrors(mangaDexResponse));
    }

    public static Result ToResult(this ValidationResult validationResult) =>
        validationResult.IsValid
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

    private static IEnumerable<Error> GetErrors(MangaDexRoot mangaDexErrors)
        => mangaDexErrors.Errors.Select(x => new Error($"{x.Status} - {x.Title} - {x.Detail}"));
}

public record ErrorInfo(string Message, int Number, int Depth, ErrorType Type);

public enum ErrorType { Regular, Exception }
