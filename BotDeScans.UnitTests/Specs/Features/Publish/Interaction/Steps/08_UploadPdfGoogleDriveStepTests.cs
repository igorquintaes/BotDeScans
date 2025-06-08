using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using FluentResults;
using File = Google.Apis.Drive.v3.Data.File;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class UploadPdfGoogleDriveStepTests : UnitTest
{
    private readonly UploadPdfGoogleDriveStep step;

    public UploadPdfGoogleDriveStepTests()
    {
        fixture.Freeze<State>();
        fixture.FreezeFake<GoogleDriveService>();
        step = fixture.Create<UploadPdfGoogleDriveStep>();
    }

    public class Properties : UploadPdfGoogleDriveStepTests
    {
        [Fact]
        public void ShouldHaveExpectedType() =>
            step.Type.Should().Be(StepType.Upload);

        [Fact]
        public void ShouldHaveExpectedName() =>
            step.Name.Should().Be(StepName.UploadPdfGoogleDrive);

        [Fact]
        public void ShouldHaveExpectedDependency() =>
            step.Dependency.Should().Be(StepName.PdfFiles);
    }

    public class ValidateAsync : UploadPdfGoogleDriveStepTests
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
                    fixture.Freeze<State>().Title.Name,
                    default,
                    cancellationToken))
                .Returns(Result.Ok(titleFolder));

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .CreateFileAsync(
                    fixture.Freeze<State>().InternalData.PdfFilePath!,
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
            fixture.Freeze<State>().ReleaseLinks.DrivePdf = null!;

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<State>().ReleaseLinks.DrivePdf.Should().Be(FILE_LINK);
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
