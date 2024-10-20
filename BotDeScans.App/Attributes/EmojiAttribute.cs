namespace BotDeScans.App.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class EmojiAttribute(string emoji) : Attribute
{
    public readonly string Emoji = $":{emoji}:";
}
