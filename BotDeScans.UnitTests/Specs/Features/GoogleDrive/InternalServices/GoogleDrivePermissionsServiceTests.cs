//using BotDeScans.App.Services.Factories;
//using BotDeScans.App.Services.GoogleDrive;
//using BotDeScans.App.Wrappers;
//using FakeItEasy;
//using FluentAssertions;
//using FluentResults;
//using FluentResults.Extensions.FluentAssertions;
//using Google.Apis.Drive.v3;
//using Google.Apis.Drive.v3.Data;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Xunit;
//using static Google.Apis.Drive.v3.PermissionsResource;

//namespace BotDeScans.UnitTests.Specs.Services.GoogleDrive
//{
//    public class GoogleDrivePermissionsServiceTests : UnitTest<GoogleDrivePermissionsService>
//    {
//        private readonly DriveService driveService;
//        private readonly GoogleDriveWrapper googleDriveWrapper;

//        public GoogleDrivePermissionsServiceTests()
//        {
//            var storageFactory = A.Fake<ExternalServicesFactory>();
//            driveService = A.Fake<DriveService>();
//            googleDriveWrapper = A.Fake<GoogleDriveWrapper>();

//            A.CallTo(() => storageFactory
//                .CreateGoogleClients())
//                .Returns(Result.Ok(driveService));

//            A.CallTo(() => driveService
//                .Permissions)
//                .Returns(A.Fake<PermissionsResource>());

//            instance = new (storageFactory, googleDriveWrapper);
//        }

//        public class CreatePublicReaderPermissionAsync : GoogleDrivePermissionsServiceTests
//        {
//            private readonly string id;
//            private readonly Result<Permission> expectedResponse;

//            public CreatePublicReaderPermissionAsync()
//            {
//                const string EXPECTED_PERMISSION_TYPE = "anyone";
//                const string EXPECTED_PERMISSION_ROLE = "reader";
//                var createRequest = A.Fake<CreateRequest>();
//                expectedResponse = new Result<Permission>();
//                id = dataGenerator.Random.Word();

//                A.CallTo(() => driveService.Permissions
//                    .Create(A<Permission>.That.Matches(permission =>
//                            permission.Type == EXPECTED_PERMISSION_TYPE &&
//                            permission.Role == EXPECTED_PERMISSION_ROLE),
//                        id))
//                    .Returns(createRequest);

//                A.CallTo(() => googleDriveWrapper
//                    .ExecuteAsync(createRequest, cancellationToken))
//                    .Returns(expectedResponse);
//            }

//            [Fact]
//            public async Task ShouldReturnPermissionFolderResult()
//            {
//                var result = await instance.CreatePublicReaderPermissionAsync(id, cancellationToken);
//                result.Should().BeSameAs(expectedResponse);
//            }
//        }

//        public class GetDriverAccessPermissionsAsync : GoogleDrivePermissionsServiceTests
//        {
//            private readonly ListRequest permissionsRequest;
//            private readonly Result<PermissionList> permissionResult;
//            private readonly string validEmailAddress;
//            private const string VALID_TYPE = "user";

//            public GetDriverAccessPermissionsAsync()
//            {
//                GoogleDriveSettingsService.BaseFolderId = dataGenerator.Random.Word();
//                var permissionsResource = A.Fake<PermissionsResource>();

//                validEmailAddress = dataGenerator.Random.Word();
//                permissionsRequest = A.Fake<ListRequest>();
//                permissionResult = Result.Ok(A.Fake<PermissionList>());

//                A.CallTo(() => driveService
//                    .Permissions)
//                    .Returns(permissionsResource);

//                A.CallTo(() => permissionsResource
//                    .List(GoogleDriveSettingsService.BaseFolderId))
//                    .Returns(permissionsRequest);

//                A.CallTo(() => googleDriveWrapper
//                    .ExecuteAsync(permissionsRequest, cancellationToken))
//                    .Returns(permissionResult);

//                A.CallTo(() => permissionResult.Value
//                    .Permissions)
//                    .Returns(new[]
//                    {
//                        new Permission { EmailAddress = validEmailAddress, Type = VALID_TYPE },
//                        new Permission { EmailAddress = validEmailAddress, Type = "invalid" },
//                        new Permission { EmailAddress = "invalid", Type = VALID_TYPE },
//                        new Permission { EmailAddress = "invalid", Type = "invalid" }
//                    });
//            }

//            [Fact]
//            public async Task ShouldReturnFilteredPermissionsAsExpected()
//            {
//                var expectedResult = Result.Ok<IEnumerable<Permission>>(new []
//                {
//                    new Permission { EmailAddress = validEmailAddress, Type = VALID_TYPE }
//                });

//                object result = await instance.GetDriverAccessPermissionsAsync(validEmailAddress, cancellationToken);
//                result.Should().BeEquivalentTo(expectedResult);
//            }

//            [Fact]
//            public async Task ShouldReturnFilteredPermissionsAsExpectedCaseInvariant()
//            {
//                var expectedResult = Result.Ok<IEnumerable<Permission>>(new[]
//                {
//                    new Permission { EmailAddress = validEmailAddress, Type = VALID_TYPE }
//                });

//                var validEmailAddressRandomCase = string.Join("", validEmailAddress
//                    .Select(x => dataGenerator.Random.Bool() 
//                        ? char.ToUpper(x) 
//                        : char.ToLower(x)));

//                object result = await instance.GetDriverAccessPermissionsAsync(validEmailAddressRandomCase, cancellationToken);
//                result.Should().BeEquivalentTo(expectedResult);
//            }

//            [Fact]
//            public async Task ShouldReturnMultipleResultsIfExists()
//            {
//                var permissions = new[]
//                {
//                    new Permission { EmailAddress = validEmailAddress, Type = VALID_TYPE },
//                    new Permission { EmailAddress = validEmailAddress, Type = VALID_TYPE },
//                    new Permission { EmailAddress = validEmailAddress, Type = "invalid" },
//                };

//                A.CallTo(() => permissionResult.Value
//                    .Permissions)
//                    .Returns(permissions);

//                var expectedResult = Result.Ok<IEnumerable<Permission>>(new[]
//                {
//                    new Permission { EmailAddress = validEmailAddress, Type = VALID_TYPE },
//                    new Permission { EmailAddress = validEmailAddress, Type = VALID_TYPE }
//                });

//                object result = await instance.GetDriverAccessPermissionsAsync(validEmailAddress, cancellationToken);
//                result.Should().BeEquivalentTo(expectedResult);
//            }

//            [Fact]
//            public async Task ShouldApplyExpectedFilters()
//            {
//                const string FIELDS_FILTER = "*";
//                const int MAX_SIZE_FILTER = 100;

//                var result = await instance.GetDriverAccessPermissionsAsync(validEmailAddress, cancellationToken);

//                A.CallTo(() => googleDriveWrapper
//                    .ExecuteAsync(
//                        A<ListRequest>.That.Matches(request => 
//                            request.Fields == FIELDS_FILTER &&
//                            request.PageSize == MAX_SIZE_FILTER), 
//                        cancellationToken))
//                    .MustHaveHappenedOnceExactly();
//            }

//            [Fact]
//            public async Task ShouldRepassExecutionError()
//            { 
//                var failResult = Result
//                    .Fail(new Error("some error")
//                    .CausedBy(dataGenerator.System.Exception()));

//                A.CallTo(() => googleDriveWrapper
//                    .ExecuteAsync(permissionsRequest, cancellationToken))
//                    .Returns(failResult);

//                object result = await instance.GetDriverAccessPermissionsAsync(validEmailAddress, cancellationToken);
//                result.Should().BeEquivalentTo(failResult);
//            }
//        }

//        public class CreateBaseUserReaderPermissionAsync : GoogleDrivePermissionsServiceTests
//        {
//            private readonly string email;
//            private readonly Result<Permission> expectedResponse;

//            public CreateBaseUserReaderPermissionAsync()
//            {
//                GoogleDriveSettingsService.BaseFolderId = dataGenerator.Random.Word();
//                const string EXPECTED_PERMISSION_TYPE = "user";
//                const string EXPECTED_PERMISSION_ROLE = "reader";
//                var createRequest = A.Fake<CreateRequest>();
//                expectedResponse = new Result<Permission>();
//                email = dataGenerator.Person.Email.ToLower();

//                A.CallTo(() => driveService.Permissions
//                    .Create(
//                        A<Permission>.That.Matches(permission =>
//                            permission.Type == EXPECTED_PERMISSION_TYPE &&
//                            permission.Role == EXPECTED_PERMISSION_ROLE &&
//                            permission.EmailAddress == email),
//                        GoogleDriveSettingsService.BaseFolderId))
//                    .Returns(createRequest);

//                A.CallTo(() => googleDriveWrapper
//                    .ExecuteAsync(createRequest, cancellationToken))
//                    .Returns(expectedResponse);
//            }

//            [Fact]
//            public async Task ShouldReturnPermissionFolderResult()
//            {
//                var result = await instance.CreateBaseUserReaderPermissionAsync(email, cancellationToken);
//                result.Should().BeSameAs(expectedResponse);
//            }
//        }

//        public class DeleteBaseUserPermissionsAsync : GoogleDrivePermissionsServiceTests
//        {
//            public readonly IEnumerable<Permission> permissions;

//            public DeleteBaseUserPermissionsAsync()
//            {
//                GoogleDriveSettingsService.BaseFolderId = dataGenerator.Random.Word();
//                permissions = new[]
//                {
//                    new Permission { Id = dataGenerator.Random.Word() },
//                    new Permission { Id = dataGenerator.Random.Word() },
//                    new Permission { Id = dataGenerator.Random.Word() }
//                };
//                var deleteRequest = A.Fake<DeleteRequest>();
//                var deleteRequestResponse = Result.Ok("some value");

//                A.CallTo(() => driveService.Permissions
//                    .Delete(
//                        GoogleDriveSettingsService.BaseFolderId,
//                        A<string>.That.Matches(id => permissions.Any(x => x.Id == id))))
//                    .Returns(deleteRequest);

//                A.CallTo(() => googleDriveWrapper
//                    .ExecuteAsync(deleteRequest, cancellationToken))
//                    .Returns(deleteRequestResponse);
//            }

//            [Fact]
//            public async Task ShouldDeletePermissionsAsExpected()
//            {
//                var result = await instance.DeleteBaseUserPermissionsAsync(permissions, cancellationToken);
//                result.Should().BeSuccess();
//            }

//            [Fact]
//            public async Task ShouldBeAbleToDealWithMultipleFailures()
//            {
//                // A successful call is still made (permissions[1])
//                var deleteRequestResponse1 = Result.Fail(new[] { "error1" });
//                var deleteRequestResponse2 = Result.Fail(new[] { "error2", "another error" });
//                var deleteRequest1 = A.Fake<DeleteRequest>();
//                var deleteRequest2 = A.Fake<DeleteRequest>();

//                A.CallTo(() => driveService.Permissions
//                    .Delete(
//                        GoogleDriveSettingsService.BaseFolderId,
//                        permissions.First().Id))
//                    .Returns(deleteRequest1);

//                A.CallTo(() => driveService.Permissions
//                    .Delete(
//                        GoogleDriveSettingsService.BaseFolderId,
//                        permissions.Last().Id))
//                    .Returns(deleteRequest2);

//                A.CallTo(() => googleDriveWrapper
//                    .ExecuteAsync(deleteRequest1, cancellationToken))
//                    .Returns(deleteRequestResponse1);

//                A.CallTo(() => googleDriveWrapper
//                    .ExecuteAsync(deleteRequest2, cancellationToken))
//                    .Returns(deleteRequestResponse2);

//                var result = await instance.DeleteBaseUserPermissionsAsync(permissions, cancellationToken);
//                result.Should().BeFailure().And
//                               .HaveError("error1").And
//                               .HaveError("error2").And
//                               .HaveError("another error");
//            }
//        }
//    }
//}
