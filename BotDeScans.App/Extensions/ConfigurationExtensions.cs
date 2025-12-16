using FluentResults;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;

namespace BotDeScans.App.Extensions;

public static class ConfigurationExtensions
{
    public static T GetRequiredValue<T>(this IConfiguration configuration, string key) =>
        configuration.GetValue<T?>(key)
        ?? throw new ArgumentNullException(nameof(key), $"'{key}' config value not found.");

    public static Result<T> GetRequiredValueAsResult<T>(this IConfiguration configuration, string key)
    {
        var value = configuration.GetValue<string?>(key, null);
        if (string.IsNullOrWhiteSpace(value))
            return Result.Fail($"'{key}' config value not found.");

        try
        {
            var typedValue = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(value)!;
            return EqualityComparer<T>.Default.Equals(typedValue, default)
                ? Result.Fail($"'{key}' config should not be filled with a valid value.")
                : Result.Ok(typedValue);
        }
        catch (NotSupportedException)
        {
            return Result.Fail($"'{key}' config value contains a not supported value.");
        }
    }

    public static T[] GetValues<T>(this IConfiguration configuration, string key, Func<string, object> parser)
    {
        var section = configuration.GetSection(key);
        if (section is null)
            return [];

        var items = section.Get<List<string>>();
        if (items is null)
            return [];

        return items
            .Where(x => x is not null)
            .Select(x => (T)parser(x))
            .Distinct()
            .ToArray();
    }
}
