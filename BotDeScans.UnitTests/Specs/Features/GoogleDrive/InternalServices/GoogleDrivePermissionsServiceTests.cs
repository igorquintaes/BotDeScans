using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services.ExternalClients;
using FluentResults;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using static Google.Apis.Drive.v3.PermissionsResource;
namespace BotDeScans.UnitTests.Specs.Features.GoogleDrive.InternalServices;

public class GoogleDrivePermissionsServiceTests : UnitTest
{
    private readonly GoogleDrivePermissionsService service;

    public GoogleDrivePermissionsServiceTests()
    {
        fixture.FreezeFake<GoogleDriveClient>();
        fixture.FreezeFake<GoogleDriveWrapper>();

        A.CallTo(() => fixture
            .FreezeFake<GoogleDriveClient>().Client)
            .Returns(fixture.FreezeFake<DriveService>());

        A.CallTo(() => fixture
            .FreezeFake<DriveService>().Permissions)
            .Returns(fixture.FreezeFake<PermissionsResource>());

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
                .FreezeFake<PermissionsResource>()
                .List(resourceId))
                .Returns(fixture.FreezeFake<ListRequest>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.FreezeFake<ListRequest>(), cancellationToken))
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
                .FreezeFake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.FreezeFake<ListRequest>(), cancellationToken))
                .Returns(fixture.FreezeFake<PermissionList>());

            var result = await service.GetUserPermissionsAsync(email, resourceId, cancellationToken);

            result.Should().BeSuccess().Which.Value.Should().BeEmpty();
        }

        [Fact]
        public async Task GivenErrorWhenRetrievingPermissionsShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.FreezeFake<ListRequest>(), cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.GetUserPermissionsAsync(email, resourceId, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task ShouldFillMandatoryRequestFields()
        {
            await service.GetUserPermissionsAsync(email, resourceId, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveWrapper>()
                .ExecuteAsync(
                    A<ListRequest>.That.Matches(x => x.Fields == "*"),
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }
    }

    public class CreatePublicReaderPermissionAsync : GoogleDrivePermissionsServiceTests
    {
        private readonly string resourceId;

        public CreatePublicReaderPermissionAsync()
        {
            resourceId = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<PermissionsResource>()
                .Create(A<Permission>.That.Matches(permission =>
                    permission.Type == GoogleDrivePermissionsService.PUBLIC_PERMISSION_TYPE &&
                    permission.Role == GoogleDrivePermissionsService.READER_ROLE),
                    resourceId))
                .Returns(fixture.FreezeFake<CreateRequest>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResultAndPermissionData()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.FreezeFake<CreateRequest>(), cancellationToken))
                .Returns(fixture.FreezeFake<Permission>());

            var result = await service.CreatePublicReaderPermissionAsync(resourceId, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.FreezeFake<Permission>());
        }

        [Fact]
        public async Task GivenErrorWhenCreatingPermissionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.FreezeFake<CreateRequest>(), cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.CreatePublicReaderPermissionAsync(resourceId, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }

    public class CreateUserReaderPermissionAsync : GoogleDrivePermissionsServiceTests
    {
        private readonly string email;
        private readonly string resourceId;

        public CreateUserReaderPermissionAsync()
        {
            email = fixture.Create<string>();
            resourceId = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<PermissionsResource>()
                .Create(A<Permission>.That.Matches(permission =>
                    permission.Type == GoogleDrivePermissionsService.USER_PERMISSION_TYPE &&
                    permission.Role == GoogleDrivePermissionsService.READER_ROLE &&
                    permission.EmailAddress == email),
                    resourceId))
                .Returns(fixture.FreezeFake<CreateRequest>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResultAndPermissionData()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.FreezeFake<CreateRequest>(), cancellationToken))
                .Returns(fixture.FreezeFake<Permission>());

            var result = await service.CreateUserReaderPermissionAsync(email, resourceId, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.FreezeFake<Permission>());
        }

        [Fact]
        public async Task GivenErrorWhenCreatingPermissionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.FreezeFake<CreateRequest>(), cancellationToken))
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
                .FreezeFake<PermissionsResource>()
                .Delete(resourceId, A<string>.That.Matches(permissionId => permissions.Select(x => x.Id).Contains(permissionId))))
                .Returns(fixture.FreezeFake<DeleteRequest>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.FreezeFake<DeleteRequest>(), cancellationToken))
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
                .FreezeFake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.FreezeFake<DeleteRequest>(), cancellationToken))
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
                .FreezeFake<GoogleDriveWrapper>()
                .ExecuteAsync(fixture.FreezeFake<DeleteRequest>(), cancellationToken))
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