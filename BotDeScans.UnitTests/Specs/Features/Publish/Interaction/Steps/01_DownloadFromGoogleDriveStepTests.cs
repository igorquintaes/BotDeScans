using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class DownloadFromGoogleDriveStepTests : UnitTest
{
    private readonly DownloadFromGoogleDriveStep step;

    public DownloadFromGoogleDriveStepTests()
    {
        fixture.FreezeFake<IPublishContext>();
        fixture.FreezeFake<FileReleaseService>();
        fixture.FreezeFake<GoogleDriveService>();

        step = fixture.Create<DownloadFromGoogleDriveStep>();
    }

    public class Properties : DownloadFromGoogleDriveStepTests
    {
        [Fact]
        public void ShouldHaveExpectedType() =>
            step.Type.Should().Be(StepType.Management);

        [Fact]
        public void ShouldHaveExpectedName() =>
            step.Name.Should().Be(StepName.Download);

        [Fact]
        public void ShouldHaveExpectedIsMandatory() =>
            step.IsMandatory.Should().Be(true);
    }

    public class ExecuteAsync : DownloadFromGoogleDriveStepTests
    {
        private readonly string[] scopedDirectories;

        public ExecuteAsync()
        {
            scopedDirectories = [.. fixture.CreateMany<string>(2)];
            var chapterInfo = fixture.Create<Info>();

            A.CallTo(() => fixture
                .FreezeFake<FileReleaseService>()
                .CreateScopedDirectory())
                .ReturnsNextFromSequence(scopedDirectories);

            A.CallTo(() => fixture
                .FreezeFake<IPublishContext>()
                .ChapterInfo)
                .Returns(chapterInfo);

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .SaveFilesAsync(
                    chapterInfo.GoogleDriveUrl.Id,
                    scopedDirectories[0],
                    cancellationToken))
                .Returns(Result.Ok());

            A.CallTo(() => fixture
                .FreezeFake<FileReleaseService>()
                .MoveCoverFile(
                    scopedDirectories[0],
                    scopedDirectories[1]))
                .Returns(fixture.Create<string>());
        }

        [Fact]
        public async Task GivenSuccessfulExecitionShouldReturnSuccessResult()
        {
            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecitionShouldSetContextData()
        {
            await step.ExecuteAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IPublishContext>()
                .SetOriginContentFolder(scopedDirectories[0]))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<IPublishContext>()
                .SetCoverFilePath(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorToSaveFilesShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .SaveFilesAsync(
                    A<string>.Ignored,
                    A<string>.Ignored,
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
