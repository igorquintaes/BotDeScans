using BotDeScans.App.Extensions;
using BotDeScans.App.Models.Entities;
using FluentValidation;

namespace BotDeScans.App.Features.References.Update;

public record Request(int TitleId, ExternalReference ReferenceKey, string ReferenceRawValue)
{
    public const int GUID_CHAR_LENGHT = 36;
    public const string MANGADEX_ID_URL_PREFIX = "/title/";

    // We can use a switch here when references get more complex (non guid)
    public string ReferenceValue => TryParseMangaDexReference(ReferenceRawValue, out var value) ?
        value.ToString() : ReferenceRawValue;

    public static bool TryParseMangaDexReference(string input, out Guid value)
    {
        if (Guid.TryParse(input, out value))
            return true;

        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri) || uri.Authority != "mangadex.org")
            return false;

        var index = input.IndexOf(MANGADEX_ID_URL_PREFIX, StringComparison.Ordinal);
        if (index == -1 || input.Length < index + MANGADEX_ID_URL_PREFIX.Length + GUID_CHAR_LENGHT)
            return false;

        var guidSpan = input.AsSpan(index + MANGADEX_ID_URL_PREFIX.Length, GUID_CHAR_LENGHT);
        return Guid.TryParse(guidSpan, out value);
    }
}

public class RequestValidator : AbstractValidator<Request>
{
    public RequestValidator()
    {
        RuleFor(request => request.TitleId).NotEmpty();

        RuleFor(request => request.ReferenceKey).IsInEnum();

        RuleFor(request => request.ReferenceRawValue).NotEmpty();

        RuleFor(request => request.ReferenceRawValue)
            .Must((request, referenceRawValue) => Request.TryParseMangaDexReference(referenceRawValue, out _))
            .When(request => (request.ReferenceKey is ExternalReference.MangaDex or ExternalReference.SakuraMangas)
                          && string.IsNullOrWhiteSpace(request.ReferenceRawValue) is false)
            .WithMessage(request =>
                          $"Valor de referência inválida para {request.ReferenceKey.GetDescription()}. " +
                          $"É necessário o ID da obra ou o link da página da obra.");
    }
}