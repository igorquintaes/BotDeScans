using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using Box.Sdk.Gen.Schemas;
using File = Box.Sdk.Gen.Schemas.File;
using Task = System.Threading.Tasks.Task;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class UploadPdfBoxStepTests : UnitTest
{
    private readonly UploadPdfBoxStep step;
    private readonly State state;

    public UploadPdfBoxStepTests()
    {
        fixture.FreezeFake<BoxService>();

        state = new State
        {
            Title = fixture.Create<Title>(),
            PdfFilePath = fixture.Create<string>()
        };

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
            var result = await step.ValidateAsync(state, cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ExecuteAsync : UploadPdfBoxStepTests
    {
        private const string FILE_LINK = "http://www.escoladescans.com/sample.pdf";
        private readonly FolderMini titleFolder;

        public ExecuteAsync()
        {
            var folderId = fixture.Create<string>();
            var sharedLink = fixture.CreateCustom<FileSharedLinkField>(f => f
                .With(x => x.DownloadUrl, FILE_LINK));

            titleFolder = fixture.CreateCustom<FolderMini>(f => f
                .With(x => x.Id, folderId));

            var titleFile = fixture.CreateCustom<File>(f => f
                .With(x => x.SharedLink, sharedLink));

            A.CallTo(() => fixture
                .FreezeFake<BoxService>()
                .GetOrCreateFolderAsync(
                    state.Title.Name,
                    cancellationToken))
                .Returns(titleFolder);

            A.CallTo(() => fixture
                .FreezeFake<BoxService>()
                .CreateFileAsync(
                    state.PdfFilePath!,
                    titleFolder.Id,
                    cancellationToken))
                .Returns(titleFile);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSetBoxPdfContextValue()
        {
            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Value.BoxPdfLink.Should().Be(FILE_LINK);
        }

        [Fact]
        public async Task GivenPdfLinkShouldConvertItIntoAReaderKey()
        {
            const string EXPECTED_KEY = "sample";

            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Value.BoxPdfReaderKey.Should().Be(EXPECTED_KEY);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallGetOrCreateFolderWithCorrectParameters()
        {
            await step.ExecuteAsync(state, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<BoxService>()
                .GetOrCreateFolderAsync(
                    A<string>.Ignored,
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallCreateFileAsyncWithCorrectParameters()
        {
            await step.ExecuteAsync(state, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<BoxService>()
                .CreateFileAsync(
                    A<string>.Ignored,
                    titleFolder.Id,
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }
    }
}
