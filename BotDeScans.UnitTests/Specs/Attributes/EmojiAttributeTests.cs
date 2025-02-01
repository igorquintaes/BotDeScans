using AutoFixture;
using BotDeScans.App.Attributes;
using FluentAssertions;
using Xunit;
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
