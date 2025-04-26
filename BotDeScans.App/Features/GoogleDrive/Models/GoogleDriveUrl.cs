using BotDeScans.App.Features.GoogleDrive.InternalServices;
using FluentResults;
using FluentValidation;
using static BotDeScans.App.Features.Publish.PublishState;
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

public partial class InfoValidator : AbstractValidator<Info>
{
    public InfoValidator(
        GoogleDriveFilesService googleDriveFilesService,
        IValidator<IList<File>> driveFilesValidator)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(model => model.GoogleDriveUrl)
            .Must(prop => Uri.TryCreate(prop.Url, UriKind.Absolute, out var uri) && uri.Authority == "drive.google.com")
            .WithMessage("O link informado é inválido.")
            .Must(prop => prop.Id.Length == 33)
            .WithMessage("O link informado é inválido.");

        Result<IList<File>> filesResult = Result.Ok();
        RuleFor(model => model.GoogleDriveUrl)
            .MustAsync(async (prop, cancellationToken) =>
            {
                filesResult = await googleDriveFilesService.GetManyAsync(prop.Id, cancellationToken);
                return filesResult.IsSuccess;
            })
            .WithMessage(string.Join("; ", filesResult.Errors.Select(error => error.Message)));

        RuleFor(_ => filesResult.Value)
            .SetValidator(driveFilesValidator);
    }
}