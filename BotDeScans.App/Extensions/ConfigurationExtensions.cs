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

        return Result.Try(
            () => (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(value)!,
             _ => new Error($"'{key}' config value contains a not supported value."));

    }

    public static T[] GetValues<T>(this IConfiguration configuration, string key) =>
        configuration.GetSection(key)
                    ?.Get<List<T>>()
                    ?.Distinct()
                     .ToArray()
                    ?? [];
}
