using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services;
using Box.V2.Models;
namespace BotDeScans.UnitTests.Specs.Features.Publish.Steps;

public class UploadPdfBoxStepTests : UnitTest
{
    private readonly IStep step;

    public UploadPdfBoxStepTests()
    {
        fixture.Freeze<PublishState>();
        fixture.FreezeFake<BoxService>();
        step = fixture.Create<UploadPdfBoxStep>();
    }

    public class Properties : UploadPdfBoxStepTests
    {
        [Fact]
        public void ShouldHaveExpectedName() =>
            step.Name.Should().Be(StepName.UploadPdfBox);

        [Fact]
        public void ShouldHaveExpectedType() =>
            step.StepType.Should().Be(StepType.Upload);
    }

    public class ValidateBeforeFilesManagementAsync : UploadPdfBoxStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateBeforeFilesManagementAsync(cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ValidateAfterFilesManagementAsync : UploadPdfBoxStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateAsync(cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ExecuteAsync : UploadPdfBoxStepTests
    {
        private const string FILE_LINK = "http://www.escoladescans.com/sample";

        public ExecuteAsync()
        {
            fixture.FreezeFake<BoxFolder>();
            fixture.FreezeFake<BoxFile>();

            A.CallTo(() => fixture.FreezeFake<BoxFolder>().Id)
                .Returns("box-folder-id");

            A.CallTo(() => fixture.FreezeFake<BoxFile>().SharedLink.DownloadUrl)
                .Returns(FILE_LINK);

            A.CallTo(() => fixture
                .FreezeFake<BoxService>()
                .GetOrCreateFolderAsync(fixture.Freeze<PublishState>().Title.Name, "0"))
                .Returns(fixture.FreezeFake<BoxFolder>());

            A.CallTo(() => fixture
                .FreezeFake<BoxService>()
                .CreateFileAsync(
                    fixture.Freeze<PublishState>().InternalData.PdfFilePath!,
                    fixture.FreezeFake<BoxFolder>().Id))
                .Returns(fixture.FreezeFake<BoxFile>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSetBoxPdfStateValue()
        {
            fixture.Freeze<PublishState>().ReleaseLinks.BoxPdf = null!;

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<PublishState>().ReleaseLinks.BoxPdf.Should().Be(FILE_LINK);
        }

        [Fact]
        public async Task GivenPdfLinkShouldConvertItIntoAReaderKey()
        {
            const string LINK = "http://escoladescans.com/sample.pdf";
            const string EXPECTED_KEY = "sample";

            fixture.Freeze<PublishState>().ReleaseLinks.BoxPdfReaderKey = null!;

            A.CallTo(() => fixture.FreezeFake<BoxFile>().SharedLink.DownloadUrl)
                .Returns(LINK);

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<PublishState>().ReleaseLinks.BoxPdfReaderKey.Should().Be(EXPECTED_KEY);
        }
    }
}
