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
    private readonly State state;

    public DownloadFromGoogleDriveStepTests()
    {
        fixture.FreezeFake<FileReleaseService>();
        fixture.FreezeFake<GoogleDriveService>();

        state = new State
        {
            ChapterInfo = fixture.Create<Info>()
        };

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
        private readonly string coverPath;

        public ExecuteAsync()
        {
            scopedDirectories = [.. fixture.CreateMany<string>(2)];
            coverPath = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<FileReleaseService>()
                .CreateScopedDirectory())
                .ReturnsNextFromSequence(scopedDirectories);

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .SaveFilesAsync(
                    state.ChapterInfo.GoogleDriveUrl.Id,
                    scopedDirectories[0],
                    cancellationToken))
                .Returns(Result.Ok());

            A.CallTo(() => fixture
                .FreezeFake<FileReleaseService>()
                .MoveCoverFile(
                    scopedDirectories[0],
                    scopedDirectories[1]))
                .Returns(coverPath);
        }

        [Fact]
        public async Task GivenSuccessfulExecitionShouldReturnSuccessResult()
        {
            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecitionShouldSetContextData()
        {
            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Value.OriginContentFolder.Should().Be(scopedDirectories[0]);
            result.Value.CoverFilePath.Should().Be(coverPath);
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

            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
