using BotDeScans.App.Attributes;
using System.ComponentModel;
using System.Reflection;
namespace BotDeScans.App.Extensions;

public static class EnumExtensions
{
    public static string GetDescription<TEnum>(this TEnum value) where TEnum : Enum =>
        GetAttribute<DescriptionAttribute, TEnum>(value).Description;

    public static string GetEmoji<TEnum>(this TEnum value) where TEnum : Enum =>
        GetAttribute<EmojiAttribute, TEnum>(value).Emoji;

    private static T GetAttribute<T, TEnum>(TEnum obj) where T : Attribute where TEnum : Enum =>
        obj.GetType()
           .GetMember(obj.ToString())
           .First()
           .GetCustomAttribute<T>()
        ?? throw new InvalidOperationException($"Attribute not found. Attr Type: {typeof(TEnum)}, object value: {obj}");
}
