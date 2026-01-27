using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using BotDeScans.App.Services.Initializations.Factories;
using Box.Sdk.Gen;
using Box.Sdk.Gen.Managers;
using Box.Sdk.Gen.Schemas;
using Microsoft.Extensions.Configuration;
using Task = System.Threading.Tasks.Task;

namespace BotDeScans.UnitTests.Specs.Services.Initializations.Factories;

public abstract class BoxClientFactoryTests : UnitTest
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

    public class CreateAsync : BoxClientFactoryTests
    {
        [Fact]
        public async Task GivenExecutionShouldReturnExpectedResult()
        {
            fixture.FreezeFakeConfiguration("Box:ClientId", fixture.Create<string>());
            fixture.FreezeFakeConfiguration("Box:ClientSecret", fixture.Create<string>());

            var result = await factory.CreateAsync(cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class HealthCheckAsync : BoxClientFactoryTests
    {
        private readonly FolderFull folder;

        public HealthCheckAsync()
        {
            fixture.FreezeFake<IBoxClient>();

            var itemCollection = fixture.CreateCustom<Items>(f => f
                .With(x => x.TotalCount, 1));

            folder = fixture.CreateCustom<FolderFull>(f => f
                .With(x => x.ItemCollection, itemCollection));

            A.CallTo(() => fixture
                .FreezeFake<IBoxClient>()
                .Folders.GetFolderByIdAsync(
                    BoxService.ROOT_ID,
                    A<GetFolderByIdQueryParams?>.Ignored,
                    A<GetFolderByIdHeaders?>.Ignored,
                    cancellationToken))
                .Returns(folder);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await factory.HealthCheckAsync(fixture.FreezeFake<IBoxClient>(), cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenUnexpectedCountValueShouldReturnFailResult()
        {
            var invalidItemCollection = fixture.CreateCustom<Items>(f => f
                .With(x => x.TotalCount, 0));

            var invalidFolder = fixture.CreateCustom<FolderFull>(f => f
                .With(x => x.ItemCollection, invalidItemCollection));

            A.CallTo(() => fixture
                .FreezeFake<IBoxClient>()
                .Folders.GetFolderByIdAsync(
                    BoxService.ROOT_ID,
                    A<GetFolderByIdQueryParams?>.Ignored,
                    A<GetFolderByIdHeaders?>.Ignored,
                    cancellationToken))
                .Returns(invalidFolder);

            var result = await factory.HealthCheckAsync(fixture.FreezeFake<IBoxClient>(), cancellationToken);

            result.Should().BeFailure().And.HaveError("Unknown error while trying to retrieve information from account.");
        }

        [Fact]
        public async Task GivenNullFolderShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<IBoxClient>()
                .Folders.GetFolderByIdAsync(
                    BoxService.ROOT_ID,
                    A<GetFolderByIdQueryParams?>.Ignored,
                    A<GetFolderByIdHeaders?>.Ignored,
                    cancellationToken))
                .Returns(Task.FromResult<FolderFull>(null!));

            var result = await factory.HealthCheckAsync(fixture.FreezeFake<IBoxClient>(), cancellationToken);

            result.Should().BeFailure().And.HaveError("Unknown error while trying to retrieve information from account.");
        }

        [Fact]
        public async Task GivenNullItemCollectionShouldReturnFailResult()
        {
            var folderWithoutCollection = fixture.CreateCustom<FolderFull>(f => f
                .With(x => x.ItemCollection, null));

            A.CallTo(() => fixture
                .FreezeFake<IBoxClient>()
                .Folders.GetFolderByIdAsync(
                    BoxService.ROOT_ID,
                    A<GetFolderByIdQueryParams?>.Ignored,
                    A<GetFolderByIdHeaders?>.Ignored,
                    cancellationToken))
                .Returns(folderWithoutCollection);

            var result = await factory.HealthCheckAsync(fixture.FreezeFake<IBoxClient>(), cancellationToken);

            result.Should().BeFailure().And.HaveError("Unknown error while trying to retrieve information from account.");
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallGetFolderByIdWithCorrectParameters()
        {
            await factory.HealthCheckAsync(fixture.FreezeFake<IBoxClient>(), cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IBoxClient>()
                .Folders.GetFolderByIdAsync(
                    BoxService.ROOT_ID,
                    A<GetFolderByIdQueryParams?>.Ignored,
                    A<GetFolderByIdHeaders?>.Ignored,
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }
    }
}