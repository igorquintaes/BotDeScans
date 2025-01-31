using AutoFixture;
using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Models;
using BotDeScans.App.Services.ExternalClients;
using BotDeScans.UnitTests.Specs.Extensions;
using FakeItEasy;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using System.Threading.Tasks;
using Xunit;
using static Google.Apis.Drive.v3.AboutResource;
using static Google.Apis.Drive.v3.Data.About;
namespace BotDeScans.UnitTests.Specs.Features.GoogleDrive.InternalServices;

public class GoogleDriveSettingsServiceTests : UnitTest
{
    private readonly GoogleDriveSettingsService service;

    public GoogleDriveSettingsServiceTests()
    {
        fixture.Fake<GoogleDriveClient>();
        fixture.Fake<GoogleDriveWrapper>();
        fixture.Fake<GoogleDriveFoldersService>();

        service = fixture.Create<GoogleDriveSettingsService>();
    }

    public class SetUpBaseFolderAsync : GoogleDriveSettingsServiceTests
    {
        public SetUpBaseFolderAsync()
        {
            A.CallTo(() => fixture
                .Fake<GoogleDriveFoldersService>()
                .GetAsync(
                    GoogleDriveSettingsService.BASE_FOLDER_NAME,
                    GoogleDriveSettingsService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(Result.Ok<File?>(default));

            A.CallTo(() => fixture
                .Fake<GoogleDriveFoldersService>()
                .CreateAsync(
                    GoogleDriveSettingsService.BASE_FOLDER_NAME,
                    GoogleDriveSettingsService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(fixture.Fake<File>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionWithoutExistingFolderShouldReturnSuccess()
        {
            var result = await service.SetUpBaseFolderAsync(cancellationToken);

            using var _ = new AssertionScope();
            result.Should().BeSuccess();
            GoogleDriveSettingsService.BaseFolderId.Should().Be(fixture.Fake<File>().Id);

            A.CallTo(() => fixture
                .Fake<GoogleDriveFoldersService>()
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
                .Fake<GoogleDriveFoldersService>()
                .GetAsync(
                    GoogleDriveSettingsService.BASE_FOLDER_NAME,
                    GoogleDriveSettingsService.ROOT_FOLDER_NAME,
                    cancellationToken))
                .Returns(fixture.Fake<File>());

            var result = await service.SetUpBaseFolderAsync(cancellationToken);

            using var _ = new AssertionScope();
            result.Should().BeSuccess();
            GoogleDriveSettingsService.BaseFolderId.Should().Be(fixture.Fake<File>().Id);

            A.CallTo(() => fixture
                .Fake<GoogleDriveFoldersService>()
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
                .Fake<GoogleDriveFoldersService>()
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
                .Fake<GoogleDriveFoldersService>()
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
                .Fake<GoogleDriveClient>().Client)
                .Returns(fixture.Fake<DriveService>());

            A.CallTo(() => fixture
                .Fake<DriveService>().About)
                .Returns(fixture.Fake<AboutResource>());

            A.CallTo(() => fixture
                .Fake<AboutResource>().Get())
                .Returns(fixture.Fake<GetRequest>());
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
                .Fake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.Fake<GetRequest>(), cancellationToken))
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
                .Fake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.Fake<GetRequest>(), cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.GetConsumptionDataAsync(cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}