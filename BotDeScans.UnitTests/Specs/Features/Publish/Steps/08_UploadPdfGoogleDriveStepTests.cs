using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using FluentResults;
using File = Google.Apis.Drive.v3.Data.File;
namespace BotDeScans.UnitTests.Specs.Features.Publish.Steps;

public class UploadPdfGoogleDriveStepTests : UnitTest
{
    private readonly IStep step;

    public UploadPdfGoogleDriveStepTests()
    {
        fixture.Freeze<PublishState>();
        fixture.FreezeFake<GoogleDriveService>();
        step = fixture.Create<UploadPdfGoogleDriveStep>();
    }

    public class Properties : UploadPdfGoogleDriveStepTests
    {
        [Fact]
        public void ShouldHaveExpectedName() =>
            step.Name.Should().Be(StepName.UploadPdfGoogleDrive);

        [Fact]
        public void ShouldHaveExpectedType() =>
            step.StepType.Should().Be(StepType.Upload);
    }

    public class ValidateBeforeFilesManagementAsync : UploadPdfGoogleDriveStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateBeforeFilesManagementAsync(cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ValidateAfterFilesManagementAsync : UploadPdfGoogleDriveStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateAsync(cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ExecuteAsync : UploadPdfGoogleDriveStepTests
    {
        private const string FILE_LINK = "http://www.escoladescans.com/sample";

        public ExecuteAsync()
        {
            var titleFolder = fixture.Create<File>();
            var titleFile = fixture.Create<File>();
            titleFile.WebViewLink = FILE_LINK;

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .GetOrCreateFolderAsync(
                    fixture.Freeze<PublishState>().Title.Name,
                    default,
                    cancellationToken))
                .Returns(Result.Ok(titleFolder));

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .CreateFileAsync(
                    fixture.Freeze<PublishState>().InternalData.PdfFilePath!,
                    titleFolder.Id,
                    true,
                    cancellationToken))
                .Returns(Result.Ok(titleFile));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSetGoogleDrivePdfStateValue()
        {
            fixture.Freeze<PublishState>().ReleaseLinks.DrivePdf = null!;

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<PublishState>().ReleaseLinks.DrivePdf.Should().Be(FILE_LINK);
        }

        [Fact]
        public async Task GivenErrorToGetOrCreateFolderShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .GetOrCreateFolderAsync(
                    A<string>.Ignored,
                    A<string?>.Ignored,
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToCreateFileShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .CreateFileAsync(
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<bool>.Ignored,
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
