using Bogus;
using BotDeScans.App.Attributes;
using FluentAssertions;
using Xunit;

namespace BotDeScans.UnitTests.Specs.Attributes
{
    public class EmojiAttributeTests
    {
        private static readonly Faker faker = new();

        [Fact]
        public void ShouldFormatEmojiToDiscordMessages()
        {
            var emojiName = faker.Random.Word();
            new EmojiAttribute(emojiName).Emoji.Should().Be($":{emojiName}:");
        }
    }
}
