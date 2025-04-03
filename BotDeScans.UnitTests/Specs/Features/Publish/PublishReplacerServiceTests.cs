using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using BotDeScans.App.Features.Publish;
using FluentAssertions;

namespace BotDeScans.UnitTests.Specs.Features.Publish;

public class PublishReplacerServiceTests : UnitTest
{
    public class Replace : PublishReplacerServiceTests
    {
        [Theory]
        [ClassData(typeof(ReplaceTestData))]
        public void ShouldReplaceText(string key, Func<string?> value)
        {
            var replacer = ReplaceTestData.Fixture.Create<PublishReplacerService>();
            var text = $"abc{key}xyz";

            var result = replacer.Replace(text);

            result.Should().Be($"abc{value()}xyz");
        }


        [Theory]
        [ClassData(typeof(RemoveTestData))]
        public void ShouldRemoveText(string keyStart, string keyEnd, string? value)
        {
            var replacer = RemoveTestData.Fixture.Create<PublishReplacerService>();
            var text = $"abc{keyStart}something{keyEnd}xyz";
            SetPublishStateReplaceFieldsValue(RemoveTestData.Fixture.Freeze<PublishState>(), value);

            var result = replacer.Replace(text);

            result.Should().Be($"abcxyz");
        }

        [Fact]
        public void ShouldReplaceAndRemoveTogether()
        {
            fixture.Freeze<PublishState>().ReleaseLinks.BoxPdf = null;
            fixture.Freeze<PublishState>().ReleaseLinks.DriveZip = "drive-zip";

            var replacer = fixture.Create<PublishReplacerService>();

            var text = "1-!##BOX_PDF_LINK##!-1" +
                       "2-!##START_REMOVE_IF_EMPTY_BOX_PDF_LINK##!-2" +
                       "3-!##BOX_PDF_LINK##!-3" +
                       "4-!##END_REMOVE_IF_EMPTY_BOX_PDF_LINK##!-4" +
                       "5-!##GOOGLE_DRIVE_ZIP_LINK##!-5";

            var expectedText = 
                       "1--1" +
                       "2-" +
                       "-4" +
                       "5-drive-zip-5";

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
