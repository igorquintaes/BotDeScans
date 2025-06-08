using AutoFixture.AutoFakeItEasy;
using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Models.Entities;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction;

public class TextReplacerTests : UnitTest
{
    public class Replace : TextReplacerTests
    {
        private static IFixture Fixture => ReplaceTestData.Fixture;

        [Theory]
        [ClassData(typeof(ReplaceTestData))]
        public void ShouldReplaceText(string key, Func<string?> value)
        {
            var replacer = Fixture.Create<TextReplacer>();
            var text = $"abc{key}xyz";

            var result = replacer.Replace(text);

            result.Should().Be($"abc{value()}xyz");
        }

        [Theory]
        [ClassData(typeof(RemoveTestData))]
        public void ShouldRemoveText(string keyStart, string keyEnd, string? value)
        {
            var replacer = Fixture.Create<TextReplacer>();
            var text = $"abc{keyStart}something{keyEnd}xyz";
            SetPublishStateReplaceFieldsValue(Fixture.Freeze<State>(), value);

            var result = replacer.Replace(text);

            result.Should().Be($"abcxyz");
        }

        [Theory]
        [ClassData(typeof(RemoveTestData))]
        public void ShouldRemoveOnlyTags(string keyStart, string keyEnd, string? _)
        {
            var replacer = Fixture.Create<TextReplacer>();
            var text = $"abc{keyStart}something{keyEnd}xyz";
            SetPublishStateReplaceFieldsValue(Fixture.Freeze<State>(), "some-value");

            var result = replacer.Replace(text);

            result.Should().Be($"abcsomethingxyz");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("value")]
        public void ShouldRemoveWronglyOpeningTag(string? keyValue)
        {
            var replacer = Fixture.Create<TextReplacer>();
            var sampleKey = TextReplacer.ReplaceRules.First().Key;
            var openingKey = $"!##START_REMOVE_IF_EMPTY_{sampleKey}##!";
            var text = $"abc{openingKey}xyz";
            SetPublishStateReplaceFieldsValue(Fixture.Freeze<State>(), keyValue);

            var result = replacer.Replace(text);

            result.Should().Be($"abcxyz");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("value")]
        public void ShouldRemoveWronglyClosingTag(string? keyValue)
        {
            var replacer = Fixture.Create<TextReplacer>();
            var sampleKey = TextReplacer.ReplaceRules.First().Key;
            var openingKey = $"!##END_REMOVE_IF_EMPTY_{sampleKey}##!";
            var text = $"abc{openingKey}xyz";
            SetPublishStateReplaceFieldsValue(Fixture.Freeze<State>(), keyValue);

            var result = replacer.Replace(text);

            result.Should().Be($"abcxyz");
        }

        [Fact]
        public void GivenAllReplacementRulesShouldApplyAllTogether()
        {
            Fixture.Freeze<State>().ReleaseLinks.BoxPdf = null;
            Fixture.Freeze<State>().ReleaseLinks.DriveZip = "drive-zip";
            Fixture.Freeze<State>().ReleaseLinks.DrivePdf = "drive-pdf";

            var replacer = Fixture.Create<TextReplacer>();

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

            var result = replacer.Replace(text);

            result.Should().Be(expectedText);
        }

        public class ReplaceTestData : TheoryData<string, Func<string?>>
        {
            public static readonly IFixture Fixture = CreateReplacerFixture();

            public ReplaceTestData() => AddRange(TextReplacer.ReplaceRules
                .Select(x => ($"!##{x.Key}##!",
                              new Func<string?>(() => x.Value(Fixture.Freeze<State>())))));
        }

        public class RemoveTestData : TheoryData<string, string, string?>
        {
            public static readonly IFixture Fixture = CreateReplacerFixture();

            public RemoveTestData()
            {
                AddRange(TextReplacer.ReplaceRules
                    .SelectMany(x => new (string, string, string?)[]
                    {
                        ($"!##START_REMOVE_IF_EMPTY_{x.Key}##!",
                         $"!##END_REMOVE_IF_EMPTY_{x.Key}##!",
                         null),
                        ($"!##START_REMOVE_IF_EMPTY_{x.Key}##!",
                         $"!##END_REMOVE_IF_EMPTY_{x.Key}##!",
                         string.Empty),
                        ($"!##START_REMOVE_IF_EMPTY_{x.Key}##!",
                         $"!##END_REMOVE_IF_EMPTY_{x.Key}##!",
                         " ")
                    }));
            }
        }

        private static IFixture CreateReplacerFixture()
        {
            var fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
            fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            fixture.Freeze<State>();

            return fixture;
        }

        private static void SetPublishStateReplaceFieldsValue(State state, string? value)
        {
            var title = new Title
            {
                Id = state.Title.Id,
                Name = value!,
                References = state.Title.References,
                DiscordRoleId = state.Title.DiscordRoleId
            };

            state.Title = title;
            state.ChapterInfo = new(default!, value!, value!, value!, value!, default);
            state.InternalData.BloggerImageAsBase64 = value;
            state.InternalData.BoxPdfReaderKey = value;
            state.ReleaseLinks.MegaZip = value;
            state.ReleaseLinks.MegaPdf = value;
            state.ReleaseLinks.BoxZip = value;
            state.ReleaseLinks.BoxPdf = value;
            state.ReleaseLinks.DriveZip = value;
            state.ReleaseLinks.DrivePdf = value;
            state.ReleaseLinks.MangaDex = value;
            state.ReleaseLinks.SakuraMangas = value;
        }
    }
}
