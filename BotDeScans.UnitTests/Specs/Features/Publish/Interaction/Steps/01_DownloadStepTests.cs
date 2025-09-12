using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class DownloadStepTests : UnitTest
{
    private readonly DownloadStep step;

    public DownloadStepTests()
    {
        fixture.Freeze<State>();
        fixture.FreezeFake<FileReleaseService>();
        fixture.FreezeFake<GoogleDriveService>();

        step = fixture.Create<DownloadStep>();
    }

    public class Properties : DownloadStepTests
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

    public class ExecuteAsync : DownloadStepTests
    {
        public ExecuteAsync()
        {
            var scopedDirectories = fixture.CreateMany<string>(2).ToArray();

            A.CallTo(() => fixture
                .FreezeFake<FileReleaseService>()
                .CreateScopedDirectory())
                .ReturnsNextFromSequence(scopedDirectories);

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .SaveFilesAsync(
                    fixture.Freeze<State>().ChapterInfo.GoogleDriveUrl.Id,
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
        public async Task GivenSuccessfulExecitionShouldSetStateData()
        {
            fixture.Freeze<State>().InternalData.OriginContentFolder = null!;
            fixture.Freeze<State>().InternalData.CoverFilePath = null!;

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<State>().InternalData.OriginContentFolder.Should().NotBeNullOrEmpty();
            fixture.Freeze<State>().InternalData.CoverFilePath.Should().NotBeNullOrEmpty();
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
