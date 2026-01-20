using BotDeScans.App.Attributes;
using BotDeScans.App.Extensions;
using System.ComponentModel;
using static FluentAssertions.FluentActions;

namespace BotDeScans.UnitTests.Specs.Extensions;

public class EnumExtensionsTests
{
    public class GetDescription : EnumExtensionsTests
    {
        [Fact]
        public void ShouldExtractAttributeWithSuccessWhenItExists() =>
            DescriptionEnum.Valid.GetDescription().Should().Be("description-value");

        [Fact]
        public void ShouldThrowErrorWhenAttributeDoesNotExists()
        {
            var error = $"Attribute not found. Attr Type: {typeof(DescriptionEnum)}, object value: Invalid";

            Invoking(() => DescriptionEnum.Invalid.GetDescription())
                .Should().Throw<InvalidOperationException>()
                .WithMessage(error);
        }

        private enum DescriptionEnum
        {
            [Description("description-value")]
            Valid,
            Invalid
        }
    }

    public class GetEmoji : EnumExtensionsTests
    {
        [Fact]
        public void ShouldExtractAttributeWithSuccessWhenItExists() =>
            EmojiEnum.Valid.GetEmoji().Should().Be(":emoji-value:");

        [Fact]
        public void ShouldThrowErrorWhenAttributeDoesNxotExists()
        {
            var error = $"Attribute not found. Attr Type: {typeof(EmojiEnum)}, object value: Invalid";

            Invoking(() => EmojiEnum.Invalid.GetEmoji())
                .Should().Throw<InvalidOperationException>()
                .WithMessage(error);
        }

        private enum EmojiEnum
        {
            [Emoji("emoji-value")]
            Valid,
            Invalid
        }
    }
}
