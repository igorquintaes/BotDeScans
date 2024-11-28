using FluentResults;
using FluentValidation;
namespace BotDeScans.App.Extensions;

public static class ValidatorExtensions
{
    public static void AddFailure<T>(
        this ValidationContext<T> context, 
        string propertyName, 
        IList<IError> errors) 
    {
        foreach(var error in errors)
            context.AddFailure(propertyName, error.Message);
	}
}
