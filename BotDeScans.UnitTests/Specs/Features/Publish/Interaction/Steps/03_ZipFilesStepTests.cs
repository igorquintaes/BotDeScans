using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class ZipFilesStepTests : UnitTest
{
    private readonly ZipFilesStep step;

    public ZipFilesStepTests()
    {
        fixture.FreezeFake<IPublishContext>();
        fixture.FreezeFake<FileService>();
        fixture.FreezeFake<FileReleaseService>();
        step = fixture.Create<ZipFilesStep>();
    }

    public class Properties : ZipFilesStepTests
    {
        [Fact]
        public void ShouldHaveExpectedType() =>
            step.Type.Should().Be(StepType.Management);

        [Fact]
        public void ShouldHaveExpectedName() =>
            step.Name.Should().Be(StepName.ZipFiles);

        [Fact]
        public void ShouldHaveExpectedIsMandatory() =>
            step.IsMandatory.Should().Be(false);
    }

    public class ExecuteAsync : ZipFilesStepTests
    {
        public ExecuteAsync()
        {
            var scopedDirectory = fixture.Create<string>();
            var zipDirectory = fixture.Create<string>();
            var chapterInfo = fixture.Create<Info>();
            var originFolder = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<IPublishContext>().ChapterInfo)
                .Returns(chapterInfo);

            A.CallTo(() => fixture
                .FreezeFake<IPublishContext>().OriginContentFolder)
                .Returns(originFolder);

            A.CallTo(() => fixture
                .FreezeFake<FileReleaseService>()
                .CreateScopedDirectory())
                .Returns(scopedDirectory);

            A.CallTo(() => fixture
                .FreezeFake<FileService>()
                .CreateZipFile(
                    chapterInfo.ChapterNumber,
                    originFolder,
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
            await step.ExecuteAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IPublishContext>()
                .SetZipPath(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
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
