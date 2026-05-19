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
        ClassLevelCascadeMode = CascadeMode.Stop;

        var filesResult = new Result<IList<File>>();

        RuleFor(model => model)
            .Must(prop => Uri.TryCreate(prop.Url, UriKind.Absolute, out var uri) 
                       && uri.Authority == "drive.google.com"
                       && prop.Id.Length == 33)
            .WithMessage("O link informado é inválido.");

        RuleFor(model => model)
            .MustAsync(async (_, prop, context, cancellationToken) => 
                       await BeAbleToGetGoogleDriveFilesInfo(prop, context, cancellationToken));

        RuleFor(_ => filesResult.Value)
            .SetValidator(driveFilesValidator)
            .When(x => filesResult.IsSuccess);

        async Task<bool> BeAbleToGetGoogleDriveFilesInfo(
            GoogleDriveUrl googleDriveUrl,
            ValidationContext<GoogleDriveUrl> validationContext,
            CancellationToken cancellationToken)
        {
            filesResult = await googleDriveFilesService.GetManyAsync(googleDriveUrl.Id, cancellationToken);
            if (filesResult.IsSuccess)
                return true;

            var errors = string.Join("; ", filesResult.Errors.Select(error => error.Message));
            validationContext.AddFailure(errors);

            return false;
        }

    }
}