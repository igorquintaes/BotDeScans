using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Services.Wrappers;
using FluentResults;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using static Google.Apis.Drive.v3.AboutResource;
using static Google.Apis.Drive.v3.Data.About;

namespace BotDeScans.Tests.Specs.Unit.Features.GoogleDrive.InternalServices;

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
