using AutoFixture;
using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services.ExternalClients;
using BotDeScans.UnitTests.Specs.Extensions;
using FakeItEasy;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static Google.Apis.Drive.v3.FilesResource;
namespace BotDeScans.UnitTests.Specs.Features.GoogleDrive.InternalServices;

public class GoogleDriveFoldersServiceTests : UnitTest
{
    private readonly GoogleDriveFoldersService service;

    public GoogleDriveFoldersServiceTests()
    {
        fixture.Fake<GoogleDriveClient>();
        fixture.Fake<GoogleDriveWrapper>();
        fixture.Fake<GoogleDriveResourcesService>();

        A.CallTo(() => fixture
            .Fake<GoogleDriveClient>().Client)
            .Returns(fixture.Fake<DriveService>());

        A.CallTo(() => fixture
            .Fake<DriveService>().Files)
            .Returns(fixture.Fake<FilesResource>());


        service = fixture.Create<GoogleDriveFoldersService>();
    }

    public class GetAsync : GoogleDriveFoldersServiceTests
    {

        [Fact]
        public async Task GivenSuccessfulExecutionShoulReturnSuccessResultAndExpectedResource()
        {
            var folderName = fixture.Create<string>();
            var parentId = fixture.Create<string?>();
            var resources = new List<File> { new() };

            A.CallTo(() => fixture
                .Fake<GoogleDriveResourcesService>()
                .GetResourcesAsync(
                    GoogleDriveFoldersService.FOLDER_MIMETYPE,
                    default,
                    folderName,
                    parentId,
                    default,
                    1,
                    cancellationToken))
                .Returns(resources);

            var result = await service.GetAsync(folderName, parentId, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(resources.Single());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShoulReturnSuccessResultAndNullResource()
        {
            A.CallTo(() => fixture
                .Fake<GoogleDriveResourcesService>()
                .GetResourcesAsync(
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    A<int?>.Ignored,
                    A<int?>.Ignored,
                    cancellationToken))
                .Returns(new List<File>());

            var result = await service.GetAsync(
                fixture.Create<string>(),
                fixture.Create<string>(),
                cancellationToken);

            using var _ = new AssertionScope();
            result.Should().BeSuccess();
            result.Value.Should().BeNull();
        }

        [Fact]
        public async Task GivenErrorWhenObtainingResourceShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .Fake<GoogleDriveResourcesService>()
                .GetResourcesAsync(
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    A<int?>.Ignored,
                    A<int?>.Ignored,
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.GetAsync(
                fixture.Create<string>(),
                fixture.Create<string>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }

    public class CreateAsync : GoogleDriveFoldersServiceTests
    {
        private readonly string folderName;
        private readonly string parentId;

        public CreateAsync()
        {
            folderName = fixture.Create<string>();
            parentId = fixture.Create<string>();
            fixture.Inject<File>(fixture.Create<File>());

            A.CallTo(() => fixture
                .Fake<GoogleDriveResourcesService>()
                .CreateResourceObject(
                    GoogleDriveFoldersService.FOLDER_MIMETYPE,
                    folderName,
                    parentId))
                .Returns(fixture.Create<File>());

            A.CallTo(() => fixture
                .Fake<FilesResource>()
                .Create(fixture.Create<File>()))
                .Returns(fixture.Fake<CreateRequest>());

            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.Fake<CreateRequest>(), cancellationToken))
                .Returns(fixture.Fake<File>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await service.CreateAsync(folderName, parentId, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Fake<File>());
        }

        [Fact]
        public async Task GivenGoogleDriverExecutionErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";
            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.Fake<CreateRequest>(), cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.CreateAsync(folderName, parentId, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task ShouldFillCreateRequestMandatoryFields()
        {
            await service.CreateAsync(folderName, parentId, cancellationToken);

            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .ExecuteAsync(
                    A<CreateRequest>.That.Matches(x => x.Fields == "webViewLink, id"),
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }
    }
}