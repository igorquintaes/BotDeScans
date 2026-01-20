using BotDeScans.App.Extensions;
namespace BotDeScans.UnitTests.Specs.Extensions;

public abstract class StringExtensionsTests : UnitTest
{
    public class NullIfWhitespace : StringExtensionsTests
    {
        [Fact]
        public void GivenNullStringShouldReturnNull()
        {
            string? text = null;

            var result = text.NullIfWhitespace();

            result.Should().BeNull();
        }

        [Fact]
        public void GivenEmptyStringShouldReturnNull()
        {
            var text = string.Empty;

            var result = text.NullIfWhitespace();

            result.Should().BeNull();
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData("\t")]
        [InlineData("\n")]
        [InlineData("\r\n")]
        [InlineData("   \t  \n  ")]
        public void GivenWhitespaceStringShouldReturnNull(string text)
        {
            var result = text.NullIfWhitespace();

            result.Should().BeNull();
        }

        [Fact]
        public void GivenValidStringShouldReturnSameString()
        {
            const string TEXT = "Valid text";

            var result = TEXT.NullIfWhitespace();

            result.Should().Be(TEXT);
        }

        [Fact]
        public void GivenStringWithLeadingAndTrailingWhitespaceShouldReturnSameString()
        {
            const string TEXT = "  Valid text  ";

            var result = TEXT.NullIfWhitespace();

            result.Should().Be(TEXT);
        }
    }

    public class Slugify : StringExtensionsTests
    {
        [Fact]
        public void GivenNullStringShouldReturnNull()
        {
            string? text = null;

            var result = text!.Slugify();

            result.Should().BeNull();
        }

        [Fact]
        public void GivenSimpleStringShouldReturnLowercaseString()
        {
            const string TEXT = "Simple Text";
            const string EXPECTED = "simple-text";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }

        [Fact]
        public void GivenStringWithAccentsShouldRemoveAccents()
        {
            const string TEXT = "Açúcar";
            const string EXPECTED = "acucar";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }

        [Fact]
        public void GivenStringWithMultipleAccentsShouldRemoveAllAccents()
        {
            const string TEXT = "Ação é reação";
            const string EXPECTED = "acao-e-reacao";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }

        [Fact]
        public void GivenStringWithPunctuationShouldRemovePunctuation()
        {
            const string TEXT = "Hello, World!";
            const string EXPECTED = "hello-world";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }

        [Fact]
        public void GivenStringWithSymbolsShouldRemoveSymbols()
        {
            const string TEXT = "Test @ Symbol # Here $";
            const string EXPECTED = "test-symbol-here";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }

        [Fact]
        public void GivenStringWithMixedCharactersShouldRemoveAccentsPunctuationAndSymbols()
        {
            const string TEXT = "Título do Capítulo: 123!";
            const string EXPECTED = "titulo-do-capitulo-123";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }

        [Fact]
        public void GivenStringWithNumbersShouldPreserveNumbers()
        {
            const string TEXT = "Chapter 123";
            const string EXPECTED = "chapter-123";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }

        [Fact]
        public void GivenStringWithMultipleSpacesShouldNormalizeToSingleSpace()
        {
            const string TEXT = "Multiple   Spaces   Here";
            const string EXPECTED = "multiple-spaces-here";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }

        [Fact]
        public void GivenStringWithSpecialCharactersShouldRemoveThem()
        {
            const string TEXT = "Test™®©";
            const string EXPECTED = "test";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }

        [Fact]
        public void GivenStringWithCombinedDiacriticalMarksShouldRemoveThem()
        {
            const string TEXT = "Café";
            const string EXPECTED = "cafe";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }

        [Fact]
        public void GivenStringWithPortugueseCharactersShouldNormalizeCorrectly()
        {
            const string TEXT = "São Paulo - Ação, Reação & Emoção!";
            const string EXPECTED = "sao-paulo-acao-reacao-emocao";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }

        [Fact]
        public void GivenEmptyStringShouldReturnEmptyString()
        {
            var text = string.Empty;

            var result = text.Slugify();

            result.Should().Be(string.Empty);
        }

        [Fact]
        public void GivenStringWithOnlyPunctuationShouldReturnEmpty()
        {
            const string TEXT = "!!!???...";

            var result = TEXT.Slugify();

            result.Should().BeEmpty();
        }

        [Fact]
        public void GivenStringWithJapaneseCharactersShouldRemoveThem()
        {
            const string TEXT = "マンガ Manga";
            const string EXPECTED = "manga";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }

        [Fact]
        public void GivenStringWithCyrillicCharactersShouldRemoveThem()
        {
            const string TEXT = "Привет Hello";
            const string EXPECTED = "hello";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }

        [Fact]
        public void GivenStringWithMixedCasingShouldConvertToLowercase()
        {
            const string TEXT = "ThIs Is MiXeD CaSe";
            const string EXPECTED = "this-is-mixed-case";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }

        [Fact]
        public void GivenStringWithUnderscoreShouldRemoveIt()
        {
            const string TEXT = "test_with_underscore";
            const string EXPECTED = "testwithunderscore";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }

        [Fact]
        public void GivenStringWithHyphensShouldPreserveThem()
        {
            const string TEXT = "test-with-hyphen";
            const string EXPECTED = "test-with-hyphen";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }

        [Fact]
        public void GivenStringWithLeadingAndTrailingSpacesShouldTrim()
        {
            const string TEXT = "   text with spaces   ";
            const string EXPECTED = "text-with-spaces";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }

        [Fact]
        public void GivenStringWithMixedNonLatinCharactersShouldRemoveAll()
        {
            const string TEXT = "Hello 你好 مرحبا Привет";
            const string EXPECTED = "hello";

            var result = TEXT.Slugify();

            result.Should().Be(EXPECTED);
        }
    }
}