using FluentResults;
using FluentValidation.Results;

namespace BotDeScans.App.Extensions;

public static class FluentValidationExtensions
{
    public static Result ToResult(this ValidationResult validationResult) =>
        validationResult.IsValid
            ? Result.Ok()
            : Result.Fail(validationResult.Errors.Select(validationError =>
                     new Error(validationError.ErrorMessage)
                        .WithMetadata(nameof(ValidationFailure.PropertyName), validationError.PropertyName)));
}
