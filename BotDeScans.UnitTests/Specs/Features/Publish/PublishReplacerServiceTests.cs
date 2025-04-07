using AutoFixture.AutoFakeItEasy;
using BotDeScans.App.Features.Publish;

namespace BotDeScans.UnitTests.Specs.Features.Publish;

public class PublishReplacerServiceTests : UnitTest
{
    public class Replace : PublishReplacerServiceTests
    {
        private static new IFixture fixture => ReplaceTestData.Fixture;

        [Theory]
        [ClassData(typeof(ReplaceTestData))]
        public void ShouldReplaceText(string key, Func<string?> value)
        {
            var replacer = fixture.Create<PublishReplacerService>();
            var text = $"abc{key}xyz";

            var result = replacer.Replace(text);

            result.Should().Be($"abc{value()}xyz");
        }

        [Theory]
        [ClassData(typeof(RemoveTestData))]
        public void ShouldRemoveText(string keyStart, string keyEnd, string? value)
        {
            var replacer = fixture.Create<PublishReplacerService>();
            var text = $"abc{keyStart}something{keyEnd}xyz";
            SetPublishStateReplaceFieldsValue(fixture.Freeze<PublishState>(), value);

            var result = replacer.Replace(text);

            result.Should().Be($"abcxyz");
        }

        [Theory]
        [ClassData(typeof(RemoveTestData))]
        public void ShouldRemoveOnlyTags(string keyStart, string keyEnd, string? _)
        {
            var replacer = fixture.Create<PublishReplacerService>();
            var text = $"abc{keyStart}something{keyEnd}xyz";
            SetPublishStateReplaceFieldsValue(fixture.Freeze<PublishState>(), "some-value");

            var result = replacer.Replace(text);

            result.Should().Be($"abcsomethingxyz");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("value")]
        public void ShouldRemoveWronglyOpeningTag(string? keyValue)
        {
            var replacer = fixture.Create<PublishReplacerService>();
            var sampleKey = PublishReplacerService.ReplaceRules.First().Key;
            var openingKey = $"!##START_REMOVE_IF_EMPTY_{sampleKey}##!";
            var text = $"abc{openingKey}xyz";
            SetPublishStateReplaceFieldsValue(fixture.Freeze<PublishState>(), keyValue);

            var result = replacer.Replace(text);

            result.Should().Be($"abcxyz");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("value")]
        public void ShouldRemoveWronglyClosingTag(string? keyValue)
        {
            var replacer = fixture.Create<PublishReplacerService>();
            var sampleKey = PublishReplacerService.ReplaceRules.First().Key;
            var openingKey = $"!##END_REMOVE_IF_EMPTY_{sampleKey}##!";
            var text = $"abc{openingKey}xyz";
            SetPublishStateReplaceFieldsValue(fixture.Freeze<PublishState>(), keyValue);

            var result = replacer.Replace(text);

            result.Should().Be($"abcxyz");
        }

        [Fact]
        public void GivenAllReplacementRulesShouldApplyAllTogether()
        {
            fixture.Freeze<PublishState>().ReleaseLinks.BoxPdf = null;
            fixture.Freeze<PublishState>().ReleaseLinks.DriveZip = "drive-zip";
            fixture.Freeze<PublishState>().ReleaseLinks.DrivePdf = "drive-pdf";

            var replacer = fixture.Create<PublishReplacerService>();

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

            public ReplaceTestData() => AddRange(PublishReplacerService.ReplaceRules
                .Select(x => ($"!##{x.Key}##!",
                              new Func<string?>(() => x.Value(Fixture.Freeze<PublishState>())))));
        }

        public class RemoveTestData : TheoryData<string, string, string?>
        {
            public static readonly IFixture Fixture = CreateReplacerFixture();

            public RemoveTestData()
            {
                AddRange(PublishReplacerService.ReplaceRules
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

            fixture.Freeze<PublishState>();

            return fixture;
        }

        private static void SetPublishStateReplaceFieldsValue(PublishState publishState, string? value)
        {
            publishState.Title = publishState.Title with { Name = value! };
            publishState.ReleaseInfo = new(default!, value, value!, value, value, default);
            publishState.InternalData.BloggerImageAsBase64 = value;
            publishState.ReleaseLinks.MegaZip = value;
            publishState.ReleaseLinks.MegaPdf = value;
            publishState.ReleaseLinks.BoxZip = value;
            publishState.ReleaseLinks.BoxPdf = value;
            publishState.ReleaseLinks.DriveZip = value;
            publishState.ReleaseLinks.DrivePdf = value;
            publishState.ReleaseLinks.MangaDexLink = value;
            publishState.ReleaseLinks.BoxPdfReaderKey = value;
        }
    }
}
