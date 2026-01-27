using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using Box.Sdk.Gen.Schemas;
using File = Box.Sdk.Gen.Schemas.File;
using Task = System.Threading.Tasks.Task;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class UploadZipBoxStepTests : UnitTest
{
    private readonly UploadZipBoxStep step;

    public UploadZipBoxStepTests()
    {
        fixture.Freeze<State>();
        fixture.FreezeFake<BoxService>();
        step = fixture.Create<UploadZipBoxStep>();
    }

    public class Properties : UploadZipBoxStepTests
    {
        [Fact]
        public void ShouldHaveExpectedType() =>
            step.Type.Should().Be(StepType.Upload);

        [Fact]
        public void ShouldHaveExpectedName() =>
            step.Name.Should().Be(StepName.UploadZipBox);

        [Fact]
        public void ShouldHaveExpectedDependency() =>
            step.Dependency.Should().Be(StepName.ZipFiles);
    }

    public class ValidateAsync : UploadZipBoxStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateAsync(cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ExecuteAsync : UploadZipBoxStepTests
    {
        private const string FILE_LINK = "http://www.escoladescans.com/sample";
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
                .GetOrCreateFolderAsync(fixture.Freeze<State>().Title.Name, cancellationToken))
                .Returns(titleFolder);

            A.CallTo(() => fixture
                .FreezeFake<BoxService>()
                .CreateFileAsync(
                    fixture.Freeze<State>().InternalData.ZipFilePath!,
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
        public async Task GivenSuccessfulExecutionShouldSetBoxZipStateValue()
        {
            fixture.Freeze<State>().ReleaseLinks.BoxZip = null!;

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<State>().ReleaseLinks.BoxZip.Should().Be(FILE_LINK);
        }
    }
}
