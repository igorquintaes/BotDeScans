using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Models;
using BotDeScans.App.Services.Wrappers;
using FluentAssertions.Execution;
using FluentResults;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using System.Reflection;
using static Google.Apis.Drive.v3.AboutResource;
using static Google.Apis.Drive.v3.Data.About;
using File = Google.Apis.Drive.v3.Data.File;
namespace BotDeScans.UnitTests.Specs.Features.GoogleDrive.InternalServices;

public class GoogleDriveSettingsServiceTests : UnitTest
{
    private readonly GoogleDriveSettingsService service;

    public GoogleDriveSettingsServiceTests()
    {
        fixture.FreezeFake<DriveService>();
        fixture.FreezeFake<GoogleWrapper>();
        fixture.FreezeFake<GoogleDriveFoldersService>();

        service = fixture.Create<GoogleDriveSettingsService>();
    }

    public class BaseFolderId : GoogleDriveSettingsServiceTests
    {
        [Fact]
        public void GivenNotDefinedBaseFolderShouldThrowException()
        {
            typeof(GoogleDriveSettingsService)
                .GetField("_baseFolderId", BindingFlags.NonPublic | BindingFlags.Static)!
                .SetValue(null, null);

            Func<string> call = () => GoogleDriveSettingsService.BaseFolderId;
            call.Should().Throw<InvalidOperationException>().WithMessage("Base folder not set.");
        }
    }

    public class SetUpBaseFolderAsync : GoogleDriveSettingsServiceTests
    {
        public SetUpBaseFolderAsync()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .GetAsync(
                    GoogleDriveSettingsService.BASE_FOLDER_NAME,
                    GoogleDriveSettingsService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(Result.Ok<File?>(default));

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .CreateAsync(
                    GoogleDriveSettingsService.BASE_FOLDER_NAME,
                    GoogleDriveSettingsService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(fixture.FreezeFake<File>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionWithoutExistingFolderShouldReturnSuccess()
        {
            var result = await service.SetUpBaseFolderAsync(cancellationToken);

            using var _ = new AssertionScope();
            result.Should().BeSuccess();
            GoogleDriveSettingsService.BaseFolderId.Should().Be(fixture.FreezeFake<File>().Id);

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .CreateAsync(
                    GoogleDriveSettingsService.BASE_FOLDER_NAME,
                    GoogleDriveSettingsService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionWithExistingFolderShouldReturnSuccess()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .GetAsync(
                    GoogleDriveSettingsService.BASE_FOLDER_NAME,
                    GoogleDriveSettingsService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(fixture.FreezeFake<File>());

            var result = await service.SetUpBaseFolderAsync(cancellationToken);

            using var _ = new AssertionScope();
            result.Should().BeSuccess();
            GoogleDriveSettingsService.BaseFolderId.Should().Be(fixture.FreezeFake<File>().Id);

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
                    GoogleDriveSettingsService.BASE_FOLDER_NAME,
                    GoogleDriveSettingsService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.SetUpBaseFolderAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenExecutionErrorWhenCreatingBaseFolderShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .CreateAsync(
                    GoogleDriveSettingsService.BASE_FOLDER_NAME,
                    GoogleDriveSettingsService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.SetUpBaseFolderAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }

    public class GetConsumptionDataAsync : GoogleDriveSettingsServiceTests
    {
        public GetConsumptionDataAsync()
        {
            A.CallTo(() => fixture
                .FreezeFake<DriveService>().About)
                .Returns(fixture.FreezeFake<AboutResource>());

            A.CallTo(() => fixture
                .FreezeFake<AboutResource>().Get())
                .Returns(fixture.FreezeFake<GetRequest>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResultAndExpectedData()
        {
            var about = fixture
                .Build<About>()
                .With(x => x.StorageQuota, fixture
                    .Build<StorageQuotaData>()
                    .With(x => x.Usage, 100L)
                    .With(x => x.Limit, 600L)
                    .Create())
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>()
                .ExecuteAsync(fixture.FreezeFake<GetRequest>(), cancellationToken))
                .Returns(about);

            var result = await service.GetConsumptionDataAsync(cancellationToken);
            result.Should().BeSuccess();
            result.Value.Should().BeEquivalentTo(new ConsumptionData(100, 500));
        }

        [Fact]
        public async Task GivenErrorWhileObtainingComsumptionDataShouldReturnError()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>()
                .ExecuteAsync(fixture.FreezeFake<GetRequest>(), cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.GetConsumptionDataAsync(cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}