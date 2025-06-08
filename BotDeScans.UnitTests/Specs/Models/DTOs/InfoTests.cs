using BotDeScans.App.Models.DTOs;

namespace BotDeScans.UnitTests.Specs.Models.DTOs;

public class InfoTests : UnitTest
{
    public new class ToString : InfoTests
    {
        [Fact]
        public void GivenInfoDataShouldReturnExpectedText()
        {
            var info = new Info(
                downloadUrl: "some-url",
                chapterName: "some-chapter-name",
                chapterNumber: "some-chapter-number",
                chapterVolume: "some-chapter-volume",
                message: "some-message",
                titleId: 42);

            var result = info.ToString();

            result.Should().Be(@"
=======================================================
DownloadUrl: some-url
ChapterName: some-chapter-name
ChapterNumber: some-chapter-number
ChapterVolume: some-chapter-volume
Message: some-message
=======================================================");
        }
    }
}
