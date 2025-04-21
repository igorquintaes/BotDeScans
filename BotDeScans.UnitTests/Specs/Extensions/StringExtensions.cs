using BotDeScans.App.Extensions;
namespace BotDeScans.UnitTests.Specs.Extensions;

public class StringExtensions : UnitTest
{
    public class TrimNumber : StringExtensions
    {
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void NullIfWhitespace(string? value) =>
            value.NullIfWhitespace().Should().BeNull();

        [Fact]
        public void GivenValuesShouldOutputExpectedResult() =>
            "a".NullIfWhitespace().Should().Be("a");
    }
}