using AutoFixture;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services.ExternalClients;
using BotDeScans.UnitTests.Specs.Extensions;
using FakeItEasy;
using FluentAssertions;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static Google.Apis.Drive.v3.PermissionsResource;

namespace BotDeScans.UnitTests.Specs.Features.GoogleDrive.InternalServices;

public class GoogleDrivePermissionsServiceTests : UnitTest
{
    private readonly GoogleDrivePermissionsService service;

    public GoogleDrivePermissionsServiceTests()
    {
        fixture.Fake<GoogleDriveClient>();
        fixture.Fake<GoogleDriveWrapper>();

        A.CallTo(() => fixture
            .Fake<GoogleDriveClient>().Client)
            .Returns(fixture.Fake<DriveService>());

        A.CallTo(() => fixture
            .Fake<DriveService>().Permissions)
            .Returns(fixture.Fake<PermissionsResource>());

        service = fixture.Create<GoogleDrivePermissionsService>();
    }

    public class GetUserPermissionsAsync : GoogleDrivePermissionsServiceTests
    {
        private readonly string email;
        private readonly string resourceId;
        private readonly Permission emailRelatedPermission;

        public GetUserPermissionsAsync()
        {
            email = fixture.Create<string>();
            resourceId = fixture.Create<string>();
            emailRelatedPermission = fixture.Build<Permission>()
                .With(x => x.EmailAddress, email)
                .With(x => x.Type, GoogleDrivePermissionsService.USER_PERMISSION_TYPE)
                .Create();

            var notRelatedPermission = fixture.Build<Permission>()
                .With(x => x.Type, GoogleDrivePermissionsService.USER_PERMISSION_TYPE)
                .Create();

            var permissionList = fixture.Build<PermissionList>()
                .With(x => x.Permissions, [emailRelatedPermission, notRelatedPermission])
                .Create();

            A.CallTo(() => fixture
                .Fake<PermissionsResource>()
                .List(resourceId))
                .Returns(fixture.Fake<ListRequest>());

            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.Fake<ListRequest>(), cancellationToken))
                .Returns(permissionList);
        }

        [Fact]
        public async Task GivenSuccessFulRequestWithEmailPermissionsShouldReturnSuccessResultWithData()
        {
            var result = await service.GetUserPermissionsAsync(email, resourceId, cancellationToken);

            result.Should().BeSuccess().Which.Value.Should().BeEquivalentTo([emailRelatedPermission]);
        }

        [Fact]
        public async Task GivenSuccessFulRequestWithoutEmailPermissionsShouldReturnSuccessResultWithEmptyData()
        {
            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.Fake<ListRequest>(), cancellationToken))
                .Returns(fixture.Fake<PermissionList>());

            var result = await service.GetUserPermissionsAsync(email, resourceId, cancellationToken);

            result.Should().BeSuccess().Which.Value.Should().BeEmpty();
        }

        [Fact]
        public async Task GivenErrorWhenRetrievingPermissionsShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.Fake<ListRequest>(), cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.GetUserPermissionsAsync(email, resourceId, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task ShouldFillMandatoryRequestFields()
        {
            await service.GetUserPermissionsAsync(email, resourceId, cancellationToken);

            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .ExecuteAsync(
                    A<ListRequest>.That.Matches(x => x.Fields == "*"), 
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }
    }

    public class CreatePublicReaderPermissionAsync : GoogleDrivePermissionsServiceTests
    {
        public static string resourceId;

        public CreatePublicReaderPermissionAsync()
        {
            resourceId = fixture.Create<string>();

            A.CallTo(() => fixture
                .Fake<PermissionsResource>()
                .Create(A<Permission>.That.Matches(permission =>
                    permission.Type == GoogleDrivePermissionsService.PUBLIC_PERMISSION_TYPE &&
                    permission.Role == GoogleDrivePermissionsService.READER_ROLE),
                    resourceId))
                .Returns(fixture.Fake<CreateRequest>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResultAndPermissionData()
        {
            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.Fake<CreateRequest>(), cancellationToken))
                .Returns(fixture.Fake<Permission>());

            var result = await service.CreatePublicReaderPermissionAsync(resourceId, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Fake<Permission>());
        }

        [Fact]
        public async Task GivenErrorWhenCreatingPermissionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.Fake<CreateRequest>(), cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.CreatePublicReaderPermissionAsync(resourceId, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }

    public class CreateUserReaderPermissionAsync : GoogleDrivePermissionsServiceTests
    {
        public static string email;
        public static string resourceId;

        public CreateUserReaderPermissionAsync()
        {
            email = fixture.Create<string>();
            resourceId = fixture.Create<string>();

            A.CallTo(() => fixture
                .Fake<PermissionsResource>()
                .Create(A<Permission>.That.Matches(permission =>
                    permission.Type == GoogleDrivePermissionsService.USER_PERMISSION_TYPE &&
                    permission.Role == GoogleDrivePermissionsService.READER_ROLE &&
                    permission.EmailAddress == email),
                    resourceId))
                .Returns(fixture.Fake<CreateRequest>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResultAndPermissionData()
        {
            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.Fake<CreateRequest>(), cancellationToken))
                .Returns(fixture.Fake<Permission>());

            var result = await service.CreateUserReaderPermissionAsync(email, resourceId, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Fake<Permission>());
        }

        [Fact]
        public async Task GivenErrorWhenCreatingPermissionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.Fake<CreateRequest>(), cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.CreateUserReaderPermissionAsync(email, resourceId, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }

    public class DeleteUserReaderPermissionsAsync : GoogleDrivePermissionsServiceTests
    {
        private readonly IEnumerable<Permission> permissions;
        private readonly string resourceId;

        public DeleteUserReaderPermissionsAsync()
        {
            permissions = fixture.CreateMany<Permission>(2);
            resourceId = fixture.Create<string>();

            A.CallTo(() => fixture
                .Fake<PermissionsResource>()
                .Delete(resourceId, A<string>.That.Matches(permissionId => permissions.Select(x => x.Id).Contains(permissionId))))
                .Returns(fixture.Fake<DeleteRequest>());

            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.Fake<DeleteRequest>(), cancellationToken))
                .Returns(fixture.Create<string>());
        }

        [Fact]
        public async Task GivenSuccessfullExecutionForAllPermissionsShouldReturnSuccessResult()
        {
            var result = await service.DeleteUserReaderPermissionsAsync(permissions, resourceId, cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenAnyErrorInExecutionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.Fake<DeleteRequest>(), cancellationToken))
                .ReturnsNextFromSequence(
                    fixture.Create<string>(), 
                    Result.Fail(ERROR_MESSAGE));

            var result = await service.DeleteUserReaderPermissionsAsync(permissions, resourceId, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenMultipleErrorsInExecutionShouldReturnFailResultAndAllDetails()
        {
            const string FIRST_ERROR_MESSAGE = "first error";
            const string SECOND_ERROR_MESSAGE = "second error";

            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.Fake<DeleteRequest>(), cancellationToken))
                .ReturnsNextFromSequence(
                    Result.Fail(FIRST_ERROR_MESSAGE), 
                    Result.Fail(SECOND_ERROR_MESSAGE));

            var result = await service.DeleteUserReaderPermissionsAsync(permissions, resourceId, cancellationToken);

            result.Should().BeFailure().And
                .HaveError(FIRST_ERROR_MESSAGE).And
                .HaveError(SECOND_ERROR_MESSAGE);
        }
    }
}