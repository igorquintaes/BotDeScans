using BotDeScans.App.Extensions;
using BotDeScans.App.Features.GoogleDrive.Models;
using FluentValidation;
using System.Text.RegularExpressions;

namespace BotDeScans.App.Models.DTOs;

public record Info
{
    public string Language => "pt-br";
    public GoogleDriveUrl GoogleDriveUrl { get; init; }
    public string? ChapterName { get; init; }
    public string ChapterNumber { get; init; }
    public string? ChapterVolume { get; init; }
    public string? Message { get; init; }
    public int TitleId { get; init; }


    public Info(
        string downloadUrl,
        string chapterName,
        string chapterNumber,
        string chapterVolume,
        string message,
        int titleId)
    {
        GoogleDriveUrl = new GoogleDriveUrl(downloadUrl);
        ChapterName = chapterName.NullIfWhitespace();
        ChapterNumber = chapterNumber;
        ChapterVolume = chapterVolume.NullIfWhitespace();
        Message = message.NullIfWhitespace();
        TitleId = titleId;
    }

    public override string ToString() => @$"
=======================================================
DownloadUrl: {GoogleDriveUrl.Url}
ChapterName: {ChapterName}
ChapterNumber: {ChapterNumber}
ChapterVolume: {ChapterVolume}
Message: {Message}
=======================================================";
}

public partial class InfoValidator : AbstractValidator<Info>
{
    public InfoValidator(IValidator<GoogleDriveUrl> googleDriveUrlValidator)
    {
        RuleFor(model => model.ChapterName)
            .Must(prop => prop!.Length <= 255)
            .When(prop => string.IsNullOrWhiteSpace(prop.ChapterName) is false)
            .WithMessage("Nome de capítulo muito longo.");

        RuleFor(model => model.ChapterNumber)
            .Must(prop => ChapterNumberPattern().Match(prop).Success)
            .WithMessage("Número do capítulo inválido.");

        RuleFor(model => model.ChapterVolume)
            .Must(prop => int.TryParse(prop, out var volume) && volume >= 0)
            .When(prop => string.IsNullOrWhiteSpace(prop.ChapterVolume) is false)
            .WithMessage("Volume do capítulo inválido.");

        RuleFor(model => model.GoogleDriveUrl)
            .SetValidator(googleDriveUrlValidator);
    }

    [GeneratedRegex("^((0|[1-9]\\d*)(\\.[1-9])?){1}$")]
    private static partial Regex ChapterNumberPattern();
}
