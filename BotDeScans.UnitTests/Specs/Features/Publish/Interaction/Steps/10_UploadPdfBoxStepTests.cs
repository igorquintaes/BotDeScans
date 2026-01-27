using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using Box.Sdk.Gen.Schemas;
using File = Box.Sdk.Gen.Schemas.File;
using Task = System.Threading.Tasks.Task;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class UploadPdfBoxStepTests : UnitTest
{
    private readonly UploadPdfBoxStep step;

    public UploadPdfBoxStepTests()
    {
        fixture.Freeze<State>();
        fixture.FreezeFake<BoxService>();
        step = fixture.Create<UploadPdfBoxStep>();
    }

    public class Properties : UploadPdfBoxStepTests
    {
        [Fact]
        public void ShouldHaveExpectedType() =>
            step.Type.Should().Be(StepType.Upload);

        [Fact]
        public void ShouldHaveExpectedName() =>
            step.Name.Should().Be(StepName.UploadPdfBox);

        [Fact]
        public void ShouldHaveExpectedDependency() =>
            step.Dependency.Should().Be(StepName.PdfFiles);
    }

    public class ValidateAsync : UploadPdfBoxStepTests
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
        private const string FILE_LINK = "http://www.escoladescans.com/sample.pdf";
        private readonly FolderMini titleFolder;
        private readonly File titleFile;

        public ExecuteAsync()
        {
            var folderId = fixture.Create<string>();
            var sharedLink = fixture.CreateCustom<FileSharedLinkField>(f => f
                .With(x => x.DownloadUrl, FILE_LINK));

            titleFolder = fixture.CreateCustom<FolderMini>(f => f
                .With(x => x.Id, folderId));

            titleFile = fixture.CreateCustom<File>(f => f
                .With(x => x.SharedLink, sharedLink));

            A.CallTo(() => fixture
                .FreezeFake<BoxService>()
                .GetOrCreateFolderAsync(
                    fixture.Freeze<State>().Title.Name,
                    cancellationToken))
                .Returns(titleFolder);

            A.CallTo(() => fixture
                .FreezeFake<BoxService>()
                .CreateFileAsync(
                    fixture.Freeze<State>().InternalData.PdfFilePath!,
                    titleFolder.Id,
                    cancellationToken))
                .Returns(titleFile);
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
            fixture.Freeze<State>().ReleaseLinks.BoxPdf = null!;

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<State>().ReleaseLinks.BoxPdf.Should().Be(FILE_LINK);
        }

        [Fact]
        public async Task GivenPdfLinkShouldConvertItIntoAReaderKey()
        {
            const string LINK = "http://escoladescans.com/sample.pdf";
            const string EXPECTED_KEY = "sample";

            fixture.Freeze<State>().InternalData.BoxPdfReaderKey = null!;

            var sharedLink = fixture.CreateCustom<FileSharedLinkField>(f => f
                .With(x => x.DownloadUrl, LINK));

            var updatedFile = fixture.CreateCustom<File>(f => f
                .With(x => x.SharedLink, sharedLink));

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<State>().InternalData.BoxPdfReaderKey.Should().Be(EXPECTED_KEY);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallGetOrCreateFolderWithCorrectParameters()
        {
            await step.ExecuteAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<BoxService>()
                .GetOrCreateFolderAsync(
                    fixture.Freeze<State>().Title.Name,
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallCreateFileAsyncWithCorrectParameters()
        {
            await step.ExecuteAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<BoxService>()
                .CreateFileAsync(
                    fixture.Freeze<State>().InternalData.PdfFilePath!,
                    titleFolder.Id,
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }
    }
}