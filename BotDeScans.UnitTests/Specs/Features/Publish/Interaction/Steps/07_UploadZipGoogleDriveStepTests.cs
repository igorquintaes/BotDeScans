using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;
using FluentResults;
using File = Google.Apis.Drive.v3.Data.File;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class UploadZipGoogleDriveStepTests : UnitTest
{
    private readonly UploadZipGoogleDriveStep step;
    private readonly State state;

    public UploadZipGoogleDriveStepTests()
    {
        fixture.FreezeFake<GoogleDriveService>();

        state = new State
        {
            Title = fixture.Create<Title>(),
            ZipFilePath = fixture.Create<string>()
        };

        step = fixture.Create<UploadZipGoogleDriveStep>();
    }

    public class Properties : UploadZipGoogleDriveStepTests
    {
        [Fact]
        public void ShouldHaveExpectedType() =>
            step.Type.Should().Be(StepType.Upload);

        [Fact]
        public void ShouldHaveExpectedName() =>
            step.Name.Should().Be(StepName.UploadZipGoogleDrive);

        [Fact]
        public void ShouldHaveExpectedDependency() =>
            step.Dependency.Should().Be(StepName.ZipFiles);
    }

    public class ValidateAsync : UploadZipGoogleDriveStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateAsync(state, cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ExecuteAsync : UploadZipGoogleDriveStepTests
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
                    state.Title.Name,
                    default,
                    cancellationToken))
                .Returns(Result.Ok(titleFolder));

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .CreateFileAsync(
                    state.ZipFilePath!,
                    titleFolder.Id,
                    true,
                    cancellationToken))
                .Returns(Result.Ok(titleFile));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSetGoogleDriveZipContextValue()
        {
            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Value.DriveZipLink.Should().Be(FILE_LINK);
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

            var result = await step.ExecuteAsync(state, cancellationToken);

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

            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
