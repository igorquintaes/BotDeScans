using BotDeScans.App.Services;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
namespace BotDeScans.UnitTests.Specs.Services;

public class ConversionServiceTests : UnitTest<ExtractionService>
{
    public ConversionServiceTests() => 
        instance = new ExtractionService();

    public class ExtractGoogleDriveIdFromLink : ConversionServiceTests
    {
        [Theory]
        [InlineData("https://drive.google.com/drive/folders/1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn?usp=sharing")]
        [InlineData("https://drive.google.com/drive/folders/1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn")]
        [InlineData("https://drive.google.com/folderview?id=1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn")]
        [InlineData("https://drive.google.com/open?id=1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn")]
        public void IsValid(string link)
        {
            using (new AssertionScope())
            {
                instance.TryExtractGoogleDriveIdFromLink(link, out var resourceId).Should().BeTrue();
                resourceId.Should().Be("1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn");
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("not a valid link")]
        [InlineData("https://random.drive.google.com/1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn")]
        [InlineData("https://drive.google.com.random/1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn")]
        [InlineData("https://drive.google.com/drive/folders/")]
        [InlineData("https://drive.google.com/drive/folders/randomValue")]
        [InlineData("https://drive.google.com/folderview")]
        [InlineData("https://drive.google.com/folderview?id=")]
        [InlineData("https://drive.google.com/folderview?id=randomValue")]
        public void IsInvalid(string link) =>
            instance.TryExtractGoogleDriveIdFromLink(link, out var _).Should().BeFalse();
    }
}
