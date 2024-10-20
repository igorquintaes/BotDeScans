using BotDeScans.App.Attributes;
using BotDeScans.App.Extensions;
using FluentAssertions;
using System;
using System.ComponentModel;
using Xunit;

namespace BotDeScans.UnitTests.Specs.Extensions
{
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
                Action action = () => DescriptionEnum.Invalid.GetDescription();
                action.Should().ThrowExactly<InvalidOperationException>().WithMessage(
                    $"Attribute not found. Attr Type: {typeof(DescriptionEnum)}, object value: {DescriptionEnum.Invalid}");
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
                Action action = () => EmojiEnum.Invalid.GetEmoji();
                action.Should().ThrowExactly<InvalidOperationException>().WithMessage(
                    $"Attribute not found. Attr Type: {typeof(EmojiEnum)}, object value: {EmojiEnum.Invalid}");
            }

            private enum EmojiEnum
            {
                [Emoji("emoji-value")]
                Valid,
                Invalid
            }
        }
    }
}
