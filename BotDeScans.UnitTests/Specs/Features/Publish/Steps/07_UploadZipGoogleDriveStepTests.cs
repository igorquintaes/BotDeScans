using AutoFixture;
using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.UnitTests.Extensions;
using FakeItEasy;
using FluentAssertions;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using Google.Apis.Drive.v3.Data;
using System;
using System.Threading.Tasks;
using Xunit;
using File = Google.Apis.Drive.v3.Data.File;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Steps;

public class UploadZipGoogleDriveStepTests : UnitTest
{
    private readonly IStep step;

    public UploadZipGoogleDriveStepTests()
    {
        fixture.Freeze<PublishState>();
        fixture.FreezeFake<IServiceProvider>();
        step = fixture.Create<UploadZipGoogleDriveStep>();
    }

    public class Properties : UploadZipGoogleDriveStepTests
    {
        [Fact]
        public void ShouldHaveExpectedName() =>
            step.StepName.Should().Be(StepEnum.UploadZipGoogleDrive);

        [Fact]
        public void ShouldHaveExpectedType() =>
            step.StepType.Should().Be(StepType.Publish);
    }

    public class ValidateBeforeFilesManagementAsync : UploadZipGoogleDriveStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateBeforeFilesManagementAsync(cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ValidateAfterFilesManagementAsync : UploadZipGoogleDriveStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateAfterFilesManagementAsync(cancellationToken);

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
                .FreezeFake<IServiceProvider>()
                .GetService(typeof(GoogleDriveService)))
                .Returns(fixture.FreezeFake<GoogleDriveService>());

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
                    fixture.Freeze<PublishState>().InternalData.ZipFilePath,
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
        public async Task GivenSuccessfulExecutionShouldSetGoogleDriveZipStateValue()
        {
            fixture.Freeze<PublishState>().ReleaseLinks.DriveZip = null!;

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<PublishState>().ReleaseLinks.DriveZip.Should().Be(FILE_LINK);
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
