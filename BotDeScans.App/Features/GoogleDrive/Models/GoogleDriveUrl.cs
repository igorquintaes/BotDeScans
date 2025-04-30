using BotDeScans.App.Features.GoogleDrive.InternalServices;
using FluentResults;
using FluentValidation;
using File = Google.Apis.Drive.v3.Data.File;
namespace BotDeScans.App.Features.GoogleDrive.Models;

public record GoogleDriveUrl(string Url)
{
    public string Id => Url
        .Replace("?id=", "/")
        .Replace("?usp=sharing", "")
        .Replace("?usp=share_link", "")
        .Split("/")
        .Last();
}

public class GoogleDriveUrlValidator : AbstractValidator<GoogleDriveUrl>
{
    public GoogleDriveUrlValidator(
        GoogleDriveFilesService googleDriveFilesService,
        IValidator<IList<File>> driveFilesValidator)
    {
        Result<IList<File>> filesResult = Result.Ok();
        RuleFor(model => model)
            .Cascade(CascadeMode.Stop)
            .Must(prop => Uri.TryCreate(prop.Url, UriKind.Absolute, out var uri) && uri.Authority == "drive.google.com")
            .WithMessage("O link informado é inválido.")
            .Must(prop => prop.Id.Length == 33)
            .WithMessage("O link informado é inválido.")
            .MustAsync(async (_, prop, context, cancellationToken) =>
            {
                filesResult = await googleDriveFilesService.GetManyAsync(prop.Id, cancellationToken);
                if (filesResult.IsSuccess)
                    return true;

                context.AddFailure(string.Join("; ", filesResult.Errors.Select(error => error.Message)));
                return false;
            });

        RuleFor(_ => filesResult.Value)
            .SetValidator(driveFilesValidator)
            .When(x => filesResult.IsSuccess);
    }
}