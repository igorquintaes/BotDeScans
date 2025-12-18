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
        var value = configuration.GetValue<string?>(key);

        if (string.IsNullOrWhiteSpace(value))
            return Result.Fail($"'{key}' config value not found.");

        try
        {
            var converter = TypeDescriptor.GetConverter(typeof(T));
            var convertedValue = (T)converter.ConvertFromInvariantString(value)!;
            return Result.Ok(convertedValue);
        }
        catch (Exception)
        {
            var errorMessage = $"'{key}' config value contains an unsupported value.";
            return Result.Fail(errorMessage);
        }
    }

    public static T[] GetValues<T>(this IConfiguration configuration, string key) =>
        configuration.GetSection(key)
                    ?.Get<List<T>>()
                    ?.Distinct()
                     .ToArray()
                    ?? [];
}
