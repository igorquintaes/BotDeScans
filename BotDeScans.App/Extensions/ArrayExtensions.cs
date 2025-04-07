namespace BotDeScans.App.Extensions;

public static class ArrayExtensions
{
    public static bool NotContains<T>(this T[] array, T element) =>
        !array.Contains(element);

    public static bool NotContainsAll<T>(this T[] array, T[] elements) =>
        !array.Any(item => Array.Exists(elements, element
            => (element is not null && item is not null && element.Equals(item))
            || (element is null && item is null)));
}