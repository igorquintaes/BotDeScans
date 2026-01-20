using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BotDeScans.App.Extensions;

public static class StringExtensions
{
    public static string? NullIfWhitespace(this string? text) =>
        string.IsNullOrWhiteSpace(text)
        ? null
        : text;

    public static string? Slugify(this string? text)
    {
        if (text is null) return text;

        var normalized = text.Normalize(NormalizationForm.FormD);

        var withoutDiacritics = new string(normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());

        var lowercase = withoutDiacritics.ToLowerInvariant();
        var asciiOnly = Regex.Replace(lowercase, @"[^a-z0-9\s-]", "");
        var spacesReplaced = Regex.Replace(asciiOnly, @"\s+", "-").Trim();
        var duplicateHyphensRemoved = Regex.Replace(spacesReplaced, @"-+", "-").Trim('-');
        return duplicateHyphensRemoved;
    }
}
