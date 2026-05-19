using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BotDeScans.App.Extensions;

public static partial class StringExtensions
{
    public static string? NullIfWhitespace(this string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text;

    public static string? Slugify(this string? text)
    {
        if (text is null) return text;

        var textWithNormalizdCharacters = text.Normalize(NormalizationForm.FormD);
        var textWithoutDiacritics = RemoveDiacritics(textWithNormalizdCharacters);
        var textInLowerCase = textWithoutDiacritics.ToLowerInvariant();
        var textWithValidCharacters = GetValidCharacters().Replace(textInLowerCase, "");
        var textWithSpacesReplaced = GetSpaces().Replace(textWithValidCharacters, "-").Trim();
        var textWithoutDuplicateHyphens = GetDuplicatesHyphens().Replace(textWithSpacesReplaced, "-").Trim('-');

        return textWithoutDuplicateHyphens;
    }

    private static string RemoveDiacritics(string text) => 
        new([.. text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) 
                             != UnicodeCategory.NonSpacingMark)]);

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex GetValidCharacters();

    [GeneratedRegex(@"\s+")]
    private static partial Regex GetSpaces();

    [GeneratedRegex(@"-+")]
    private static partial Regex GetDuplicatesHyphens();
}
