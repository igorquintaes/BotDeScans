using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Extensions;

public static class ConfigurationExtensions
{
    public static T GetRequiredValue<T>(this IConfiguration configuration, string key)
        => configuration.GetValue<T?>(key)
        ?? throw new ArgumentNullException(nameof(key), $"'{key}' config value not found.");

    public static T[] GetRequiredValues<T>(this IConfiguration configuration, string key, Func<string, object> parser)
        => configuration
            .GetRequiredSection(key)
            .AsEnumerable()
            .Where(x => x.Value is not null)
            .Select(x => (T)parser(x.Value!))
            .Distinct()
            .ToArray();
}
