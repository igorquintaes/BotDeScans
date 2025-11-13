using BotDeScans.App.Extensions;
using BotDeScans.App.Models.Entities;
using FluentValidation;

namespace BotDeScans.App.Features.References.Update;

public record Request(int TitleId, ExternalReference ReferenceKey, string ReferenceRawValue)
{
    public const int GUID_CHAR_LENGHT = 36;
    public const string MANGADEX_ID_URL_PREFIX = "/title/";

    // We can use a switch here when references get more complex (non guid)
    public string ReferenceValue =>
        Guid.TryParse(ReferenceRawValue, out var guidResult)
            ? guidResult.ToString()
            : ReferenceRawValue.Substring(
                ReferenceRawValue.IndexOf(MANGADEX_ID_URL_PREFIX) + MANGADEX_ID_URL_PREFIX.Length,
                GUID_CHAR_LENGHT);
}

public class RequestValidator : AbstractValidator<Request>
{
    public RequestValidator()
    {
        RuleFor(request => request.TitleId).NotEmpty();

        RuleFor(request => request.ReferenceKey).IsInEnum();

        RuleFor(request => request.ReferenceRawValue).NotEmpty();

        RuleFor(request => request.ReferenceRawValue)
            .Must(IsMangaValidMangaDexReference)
            .When(request => (request.ReferenceKey is ExternalReference.MangaDex or ExternalReference.SakuraMangas)
                          && string.IsNullOrWhiteSpace(request.ReferenceRawValue) is false)
            .WithMessage(request => 
                          $"Valor de referência inválida para {request.ReferenceKey.GetDescription()}. " +
                          $"É necessário o ID da obra ou o link da página da obra.");
    }

    private static bool IsMangaValidMangaDexReference(string url)
    {
        if (Guid.TryParse(url, out _))
            return true;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Authority != "mangadex.org")
            return false;

        var index = url.IndexOf(Request.MANGADEX_ID_URL_PREFIX, StringComparison.Ordinal);
        if (index == -1)
            return false;

        index += Request.MANGADEX_ID_URL_PREFIX.Length;

        if (url.Length < index + Request.GUID_CHAR_LENGHT)
            return false;

        var span = url.AsSpan(index, Request.GUID_CHAR_LENGHT);
        return Guid.TryParse(span, out _);
    }
}