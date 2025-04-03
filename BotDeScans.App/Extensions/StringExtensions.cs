using System.Globalization;
using System.Text;

namespace BotDeScans.App.Extensions;

public static class StringExtensions
{
    public static string? NullIfWhitespace(this string? text) => 
        string.IsNullOrWhiteSpace(text) 
        ? null 
        : text;

    public static string? Slugfy(this string? text)
    {
        if (text is null) return text;

        return new string(text
            .ToLower()
            .Normalize(NormalizationForm.FormD)
            .Where(c => 
                CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark &&
                !char.IsPunctuation(c) &&
                !char.IsSymbol(c))
            .ToArray())
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }
}
