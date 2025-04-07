using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Services;
using FluentResults;
namespace BotDeScans.UnitTests.Specs.Features.Publish.Steps;

public class DownloadStepTests : UnitTest
{
    private readonly IStep step;

    public DownloadStepTests()
    {
        fixture.Freeze<PublishState>();
        fixture.FreezeFake<FileReleaseService>();
        fixture.FreezeFake<GoogleDriveService>();

        step = fixture.Create<DownloadStep>();
    }

    public class Properties : DownloadStepTests
    {
        [Fact]
        public void ShouldHaveExpectedName() =>
            step.StepName.Should().Be(StepEnum.Download);

        [Fact]
        public void ShouldHaveExpectedType() =>
            step.StepType.Should().Be(StepType.Management);
    }

    public class ValidateBeforeFilesManagementAsync : DownloadStepTests
    {
        public ValidateBeforeFilesManagementAsync()
        {
            var folderId = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .GetFolderIdFromUrl(fixture.Freeze<PublishState>().ReleaseInfo.DownloadUrl))
                .Returns(Result.Ok(folderId));

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .ValidateFilesAsync(folderId, cancellationToken))
                .Returns(Result.Ok());
        }

        [Fact]
        public async Task GivenSuccessfulExecitionShouldReturnSuccessResult()
        {
            var result = await step.ValidateBeforeFilesManagementAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecitionShouldSetGoogleDriveFolderId()
        {
            fixture.Freeze<PublishState>().InternalData.GoogleDriveFolderId = null!;

            await step.ValidateBeforeFilesManagementAsync(cancellationToken);

            fixture.Freeze<PublishState>().InternalData.GoogleDriveFolderId.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task GivenErrorWhenTryingToGetFolderIdShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .GetFolderIdFromUrl(A<string>.Ignored))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ValidateBeforeFilesManagementAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorWhenTryingToValidateFilesShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .ValidateFilesAsync(A<string>.Ignored, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ValidateBeforeFilesManagementAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }

    public class ValidateAfterFilesManagementAsync : DownloadStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateAfterFilesManagementAsync(cancellationToken);

            result.Should().BeSuccess();
        }
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
                    fixture.Freeze<PublishState>().InternalData.GoogleDriveFolderId,
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
            fixture.Freeze<PublishState>().InternalData.OriginContentFolder = null!;
            fixture.Freeze<PublishState>().InternalData.CoverFilePath = null!;

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<PublishState>().InternalData.OriginContentFolder.Should().NotBeNullOrEmpty();
            fixture.Freeze<PublishState>().InternalData.CoverFilePath.Should().NotBeNullOrEmpty();
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
