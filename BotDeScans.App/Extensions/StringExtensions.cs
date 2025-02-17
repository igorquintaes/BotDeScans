namespace BotDeScans.App.Extensions;

public static class StringExtensions
{
    public static string? NullIfWhitespace(this string? text) => 
        string.IsNullOrWhiteSpace(text) 
        ? null 
        : text;
}
