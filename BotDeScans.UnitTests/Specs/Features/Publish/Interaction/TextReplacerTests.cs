using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Models.Entities;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction;

public class TextReplacerTests : UnitTest
{
    public class Replace : TextReplacerTests
    {
        private readonly TextReplacer replacer = new();
        private readonly State state;

        public Replace()
        {
            state = new State
            {
                Title = fixture.Create<Title>(),
                ChapterInfo = fixture.Create<Info>(),
                BloggerImageAsBase64 = fixture.Create<string>(),
                BoxPdfReaderKey = fixture.Create<string>(),
                MegaZipLink = fixture.Create<string>(),
                MegaPdfLink = fixture.Create<string>(),
                BoxZipLink = fixture.Create<string>(),
                BoxPdfLink = fixture.Create<string>(),
                DriveZipLink = fixture.Create<string>(),
                DrivePdfLink = fixture.Create<string>(),
                MangaDexLink = fixture.Create<string>(),
                SakuraMangasLink = fixture.Create<string>()
            };
        }

        [Theory]
        [MemberData(nameof(GetReplaceRuleKeys))]
        public void ShouldReplaceText(string key)
        {
            var text = $"abc!##{key}##!xyz";
            var expectedValue = TextReplacer.ReplaceRules[key](state);

            var result = replacer.Replace(text, state);

            result.Should().Be($"abc{expectedValue}xyz");
        }

        [Theory]
        [MemberData(nameof(GetReplaceRuleKeys))]
        public void ShouldRemoveTextWhenValueIsNull(string key)
        {
            var emptyState = CreateStateWithNullValues();
            var text = $"abc!##START_REMOVE_IF_EMPTY_{key}##!something!##END_REMOVE_IF_EMPTY_{key}##!xyz";

            var result = replacer.Replace(text, emptyState);

            result.Should().Be("abcxyz");
        }

        [Theory]
        [MemberData(nameof(GetReplaceRuleKeys))]
        public void ShouldRemoveOnlyTagsWhenValueIsPresent(string key)
        {
            var text = $"abc!##START_REMOVE_IF_EMPTY_{key}##!something!##END_REMOVE_IF_EMPTY_{key}##!xyz";

            var result = replacer.Replace(text, state);

            result.Should().Be("abcsomethingxyz");
        }

        [Fact]
        public void GivenAllReplacementRulesShouldApplyAllTogether()
        {
            var testState = state with
            {
                BoxPdfLink = null,
                DriveZipLink = "drive-zip",
                DrivePdfLink = "drive-pdf"
            };

            var text = "1-!##BOX_PDF_LINK##!-1" +
                       "2-!##START_REMOVE_IF_EMPTY_BOX_PDF_LINK##!-2" +
                       "3-!##BOX_PDF_LINK##!-3" +
                       "4-!##END_REMOVE_IF_EMPTY_BOX_PDF_LINK##!-4" +
                       "5-!##START_REMOVE_IF_EMPTY_GOOGLE_DRIVE_PDF_LINK##!-5" +
                       "6-!##GOOGLE_DRIVE_PDF_LINK##!-6" +
                       "7-!##END_REMOVE_IF_EMPTY_GOOGLE_DRIVE_PDF_LINK##!-7" +
                       "8-!##GOOGLE_DRIVE_ZIP_LINK##!-8";

            var expectedText =
                       "1--1" +
                       "2--4" +
                       "5--5" +
                       "6-drive-pdf-6" +
                       "7--7" +
                       "8-drive-zip-8";

            var result = replacer.Replace(text, testState);

            result.Should().Be(expectedText);
        }

        public static IEnumerable<object[]> GetReplaceRuleKeys() =>
            TextReplacer.ReplaceRules.Keys.Select(k => new object[] { k });

        private State CreateStateWithNullValues() => new()
        {
            Title = new Title { Name = null!, References = [], DiscordRoleId = 0 },
            ChapterInfo = new Info(default!, null!, null!, null!, null!, default)
        };
    }
}
