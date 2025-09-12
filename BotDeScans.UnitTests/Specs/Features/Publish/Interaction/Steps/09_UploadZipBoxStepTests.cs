using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using Box.V2.Models;

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

        public ExecuteAsync()
        {
            var titleFolder = A.Fake<BoxFolder>();
            var titleFile = A.Fake<BoxFile>();

            A.CallTo(() => titleFolder.Id).Returns(nameof(titleFolder));
            A.CallTo(() => titleFile.SharedLink.DownloadUrl).Returns(FILE_LINK);

            A.CallTo(() => fixture
                .FreezeFake<BoxService>()
                .GetOrCreateFolderAsync(fixture.Freeze<State>().Title.Name, "0"))
                .Returns(titleFolder);

            A.CallTo(() => fixture
                .FreezeFake<BoxService>()
                .CreateFileAsync(
                    fixture.Freeze<State>().InternalData.ZipFilePath!,
                    titleFolder.Id))
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
