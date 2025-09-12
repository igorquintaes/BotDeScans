using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using BotDeScans.App.Services.Initializations.Factories;
using Box.V2;
using Box.V2.Managers;
using Box.V2.Models;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.UnitTests.Specs.Services.Initializations.Factories;

public class BoxClientFactoryTests : UnitTest
{
    private readonly BoxClientFactory factory;

    public BoxClientFactoryTests()
    {
        fixture.FreezeFake<IConfiguration>();

        factory = fixture.Create<BoxClientFactory>();
    }

    public class Enabled : BoxClientFactoryTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData(StepName.UploadPdfBox)]
        [InlineData(StepName.UploadZipBox)]
        public void GivenExpectedStepInsideArrayShouldReturnTrue(StepName? stepToExclude)
        {
            fixture.FreezeFakeConfiguration(
                key: "Settings:Publish:Steps",
                values: Enum
                    .GetValues<StepName>()
                    .Where(x => x != stepToExclude)
                    .Select(x => x.ToString()));

            factory.Enabled.Should().BeTrue();
        }

        [Fact]
        public void GivenNotExpectedStepShouldReturnFalse()
        {
            fixture.FreezeFakeConfiguration(
                key: "Settings:Publish:Steps",
                values: Enum
                    .GetValues<StepName>()
                    .Except([StepName.UploadPdfBox, StepName.UploadZipBox])
                    .Select(x => x.ToString()));

            factory.Enabled.Should().BeFalse();
        }
    }

    public class HealthCheckAsync : BoxClientFactoryTests
    {
        public HealthCheckAsync()
        {
            A.CallTo(() => fixture
                .FreezeFake<IBoxClient>().FoldersManager)
                .Returns(fixture.FreezeFake<IBoxFoldersManager>());

            A.CallTo(() => fixture
                .FreezeFake<IBoxFoldersManager>()
                .GetFolderItemsAsync(BoxService.ROOT_ID, 1, default, default, default, default, default, default, default))
                .Returns(new BoxCollection<BoxItem>() { TotalCount = 1 });
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await factory.HealthCheckAsync(
                fixture.FreezeFake<IBoxClient>(),
                cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenUnexpectedCountValueShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<IBoxFoldersManager>()
                .GetFolderItemsAsync(BoxService.ROOT_ID, 1, default, default, default, default, default, default, default))
                .Returns(new BoxCollection<BoxItem>() { TotalCount = 0 });

            var result = await factory.HealthCheckAsync(
                fixture.FreezeFake<IBoxClient>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError("Unknown error while trying to retrieve information from account.");
        }

        [Fact]
        public async Task GivenNullFolderitemsShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<IBoxFoldersManager>()
                .GetFolderItemsAsync(BoxService.ROOT_ID, 1, default, default, default, default, default, default, default))
                .Returns(null as BoxCollection<BoxItem>);

            var result = await factory.HealthCheckAsync(
                fixture.FreezeFake<IBoxClient>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError("Unknown error while trying to retrieve information from account.");
        }
    }
}
