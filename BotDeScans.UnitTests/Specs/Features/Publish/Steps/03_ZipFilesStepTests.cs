using BotDeScans.App.Features.Publish.State;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services;
using FluentResults;
namespace BotDeScans.UnitTests.Specs.Features.Publish.Steps;

public class ZipFilesStepTests : UnitTest
{
    private readonly ZipFilesStep step;

    public ZipFilesStepTests()
    {
        fixture.Freeze<PublishState>();
        fixture.FreezeFake<FileService>();
        fixture.FreezeFake<FileReleaseService>();
        step = fixture.Create<ZipFilesStep>();
    }

    public class Properties : ZipFilesStepTests
    {
        [Fact]
        public void ShouldHaveExpectedName() =>
            step.Name.Should().Be(StepName.ZipFiles);

        [Fact]
        public void ShouldHaveExpectedType() =>
            step.Type.Should().Be(StepType.Management);
    }

    public class ExecuteAsync : ZipFilesStepTests
    {
        public ExecuteAsync()
        {
            var scopedDirectory = fixture.Create<string>();
            var zipDirectory = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<FileReleaseService>()
                .CreateScopedDirectory())
                .Returns(scopedDirectory);

            A.CallTo(() => fixture
                .FreezeFake<FileService>()
                .CreateZipFile(
                    fixture.Freeze<PublishState>().ReleaseInfo.ChapterNumber,
                    fixture.Freeze<PublishState>().InternalData.OriginContentFolder,
                    scopedDirectory))
                .Returns(Result.Ok(zipDirectory));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSetZipFilePath()
        {
            fixture.Freeze<PublishState>().InternalData.ZipFilePath = null!;

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<PublishState>().InternalData.ZipFilePath.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task GivenErrorToCreazeZipShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<FileService>()
                .CreateZipFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
