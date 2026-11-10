using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services.Initializations;
using FluentAssertions.Execution;
using FluentResults;
using File = Google.Apis.Drive.v3.Data.File;

namespace BotDeScans.Tests.Specs.Unit.Services.Initializations;

public class GoogleDriveInitializerTests : UnitTest
{
    public class ExecuteAsync : GoogleDriveInitializerTests, IDisposable
    {
        private readonly GoogleDriveInitializer initializer;

        public ExecuteAsync()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .GetAsync(
                    GoogleDriveService.BASE_FOLDER_NAME,
                    GoogleDriveService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(Result.Ok<File?>(default));

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .CreateAsync(
                    GoogleDriveService.BASE_FOLDER_NAME,
                    GoogleDriveService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(fixture.FreezeFake<File>());

            initializer = fixture.Create<GoogleDriveInitializer>();
        }

        public void Dispose()
        {
            GoogleDriveService.BaseFolderId = null!;

            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionWithoutExistingFolderShouldReturnSuccess()
        {
            var result = await initializer.ExecuteAsync(cancellationToken);

            using var _ = new AssertionScope();
            result.Should().BeSuccess();
            GoogleDriveService.BaseFolderId.Should().Be(fixture.FreezeFake<File>().Id);

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .CreateAsync(
                    GoogleDriveService.BASE_FOLDER_NAME,
                    GoogleDriveService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionWithExistingFolderShouldReturnSuccess()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .GetAsync(
                    GoogleDriveService.BASE_FOLDER_NAME,
                    GoogleDriveService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(fixture.FreezeFake<File>());

            var result = await initializer.ExecuteAsync(cancellationToken);

            using var _ = new AssertionScope();
            result.Should().BeSuccess();
            GoogleDriveService.BaseFolderId.Should().Be(fixture.FreezeFake<File>().Id);

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .CreateAsync(
                    A<string>.Ignored,
                    A<string>.Ignored,
                    cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenExecutionErrorWhenCheckingIfBaseFolderExistsShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .GetAsync(
                    GoogleDriveService.BASE_FOLDER_NAME,
                    GoogleDriveService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await initializer.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenExecutionErrorWhenCreatingBaseFolderShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .CreateAsync(
                    GoogleDriveService.BASE_FOLDER_NAME,
                    GoogleDriveService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await initializer.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenSuccessExecutionReturnReasons()
        {
            const string REASON_1 = "reason 1";
            const string REASON_2 = "reason 2";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .GetAsync(
                    GoogleDriveService.BASE_FOLDER_NAME,
                    GoogleDriveService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(Result.Ok<File?>(default).WithSuccess(REASON_1));

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .CreateAsync(
                    GoogleDriveService.BASE_FOLDER_NAME,
                    GoogleDriveService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(Result.Ok(fixture.FreezeFake<File>()).WithSuccess(REASON_2));

            var result = await initializer.ExecuteAsync(cancellationToken);

            using var _ = new AssertionScope();
            result.Should().BeSuccess().And
                  .HaveReason(REASON_1).And
                  .HaveReason(REASON_2);
        }

        [Fact]
        public async Task GivenErrorExecutionReturnReasons()
        {
            const string REASON = "reason";
            const string ERROR = "error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .GetAsync(
                    GoogleDriveService.BASE_FOLDER_NAME,
                    GoogleDriveService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(Result.Ok<File?>(default).WithSuccess(REASON));

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .CreateAsync(
                    GoogleDriveService.BASE_FOLDER_NAME,
                    GoogleDriveService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(Result.Fail(ERROR));

            var result = await initializer.ExecuteAsync(cancellationToken);

            using var _ = new AssertionScope();
            result.Should().BeFailure().And
                  .HaveReason(REASON).And
                  .HaveError(ERROR);
        }

        [Fact]
        public async Task GivenCreateFolderErrorShouldNotSetBaseFolderId()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .CreateAsync(
                    GoogleDriveService.BASE_FOLDER_NAME,
                    GoogleDriveService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(Result.Fail("error"));

            await initializer.ExecuteAsync(cancellationToken);

            Func<string> call = () => GoogleDriveService.BaseFolderId;
            call.Should().Throw<InvalidOperationException>().WithMessage("Base folder not set.");
        }
    }
}
