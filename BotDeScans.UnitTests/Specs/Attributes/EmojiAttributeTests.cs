using BotDeScans.App.Attributes;
namespace BotDeScans.UnitTests.Specs.Attributes;

public class EmojiAttributeTests : UnitTest
{
    [Fact]
    public void ShouldFormatEmojiToDiscordMessages()
    {
        var emojiName = fixture.Create<string>();
        new EmojiAttribute(emojiName).Emoji.Should().Be($":{emojiName}:");
    }
}
