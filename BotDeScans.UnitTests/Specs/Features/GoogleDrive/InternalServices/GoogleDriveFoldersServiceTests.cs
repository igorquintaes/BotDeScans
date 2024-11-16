//using BotDeScans.App.Services.Factories;
//using BotDeScans.App.Services.GoogleDrive;
//using BotDeScans.App.Wrappers;
//using FakeItEasy;
//using FluentAssertions;
//using FluentAssertions.Execution;
//using FluentResults;
//using FluentResults.Extensions.FluentAssertions;
//using Google.Apis.Drive.v3;
//using Google.Apis.Drive.v3.Data;
//using System.Threading.Tasks;
//using Xunit;
//using static Google.Apis.Drive.v3.FilesResource;

//namespace BotDeScans.UnitTests.Specs.Services.GoogleDrive
//{
//    public class GoogleDriveFoldersServiceTests : UnitTest<GoogleDriveFoldersService>
//    {
//        private readonly DriveService driveService;
//        private readonly GoogleDriveResourcesService googleDriveResourcesService;
//        private readonly GoogleDriveWrapper googleDriveWrapper;
//        private readonly FilesResource filesResource;

//        private const string FOLDER_MIMETYPE = GoogleDriveFoldersService.FOLDER_MIMETYPE;

//        public GoogleDriveFoldersServiceTests()
//        {
//            var storageFactory = A.Fake<ExternalServicesFactory>();
//            driveService = A.Fake<DriveService>();
//            googleDriveResourcesService = A.Fake<GoogleDriveResourcesService>();
//            googleDriveWrapper = A.Fake<GoogleDriveWrapper>();
//            filesResource = A.Fake<FilesResource>();

//            A.CallTo(() => storageFactory
//                .CreateGoogleClients())
//                .Returns(Result.Ok(driveService));

//            A.CallTo(() => driveService
//                .Files)
//                .Returns(filesResource);

//            instance = new (storageFactory, googleDriveResourcesService, googleDriveWrapper);
//        }

//        public class GetFolderAsync : GoogleDriveFoldersServiceTests
//        {
//            [Fact]
//            public async Task ShouldReturnExpectedResource()
//            {
//                var expectedResult = new Result<File?>();
//                var folderName = dataGenerator.Random.Word();
//                var parentId = dataGenerator.Random.Word();

//                A.CallTo(() => googleDriveResourcesService
//                    .GetResourceByNameAsync(
//                        FOLDER_MIMETYPE,
//                        folderName,
//                        parentId,
//                        cancellationToken))
//                    .Returns(expectedResult);

//                var result = await instance.GetFolderAsync(folderName, parentId, cancellationToken);
//                result.Should().BeSameAs(expectedResult);
//            }
//        }

//        public class GetFolderByIdAsync : GoogleDriveFoldersServiceTests
//        {
//            private readonly string folderId;
//            private readonly GetRequest getRequest;

//            public GetFolderByIdAsync()
//            {
//                folderId = dataGenerator.Random.Word();
//                getRequest = A.Fake<GetRequest>();

//                A.CallTo(() => filesResource
//                    .Get(folderId))
//                    .Returns(getRequest);
//            }

//            [Fact]
//            public async Task ShouldReturnFolderWhenItExists()
//            {
//                var expectedResult = Result.Ok<File?>(new File
//                {
//                    MimeType = FOLDER_MIMETYPE
//                });

//                A.CallTo(() => googleDriveWrapper
//                    .ExecuteAsync(getRequest, cancellationToken))
//                    .Returns(expectedResult);

//                var result = await instance.GetFolderByIdAsync(folderId, cancellationToken);
//                result.Should().BeSameAs(expectedResult);
//            }

//            [Fact]
//            public async Task ShouldReturnNullWhenFolderDoesNotExists()
//            {
//                var expectedResult = Result.Ok<File?>(null);
//                A.CallTo(() => googleDriveWrapper
//                    .ExecuteAsync(getRequest, cancellationToken))
//                    .Returns(expectedResult);

//                object result = await instance.GetFolderByIdAsync(folderId, cancellationToken);
//                result.Should().BeSameAs(expectedResult);
//            }

//            [Fact]
//            public async Task ShouldReturnNullWhenResourceIsNotAFolder()
//            {
//                var returnResult = Result.Ok<File?>(new File
//                {
//                    MimeType = dataGenerator.Random.Word()
//                });

//                A.CallTo(() => googleDriveWrapper
//                    .ExecuteAsync(getRequest, cancellationToken))
//                    .Returns(returnResult);

//                var result = await instance.GetFolderByIdAsync(folderId, cancellationToken);

//                // TODO: Waiting https://github.com/altmann/FluentResults/issues/172
//                //result.Should().BeSuccess().And.HaveValue(null);

//                using (new AssertionScope())
//                {
//                    result.Should().BeSuccess();
//                    result.Value.Should().BeNull();
//                }
//            }

//            [Fact]
//            public async Task ShouldRepassErrorWhileTryingGetFolder()
//            {
//                var expectedResult = Result.Fail<File?>("some error");

//                A.CallTo(() => googleDriveWrapper
//                    .ExecuteAsync(getRequest, cancellationToken))
//                    .Returns(expectedResult);

//                var result = await instance.GetFolderByIdAsync(folderId, cancellationToken);
//                result.Should().BeSameAs(expectedResult);
//            }
//        }

//        public class CreateFolderAsync : GoogleDriveFoldersServiceTests
//        {
//            private readonly string folderName;
//            private readonly string parentId;
//            private readonly CreateRequest createRequest;

//            public CreateFolderAsync()
//            {
//                folderName = dataGenerator.Random.Word();
//                parentId = dataGenerator.Random.Word();
//                var resource = new File();
//                createRequest = A.Fake<CreateRequest>();

//                A.CallTo(() => googleDriveResourcesService
//                    .CreateResourceObject(FOLDER_MIMETYPE, folderName, parentId))
//                    .Returns(resource);

//                A.CallTo(() => filesResource
//                    .Create(resource))
//                    .Returns(createRequest);
//            }

//            [Fact]
//            public async Task ShouldReturnCreatedFolderResult()
//            {
//                var expectedResult = new Result<File>();

//                A.CallTo(() => googleDriveWrapper
//                    .ExecuteAsync(createRequest, cancellationToken))
//                    .Returns(expectedResult);

//                var result = await instance.CreateFolderAsync(folderName, parentId);
//                result.Should().BeSameAs(expectedResult);
//            }

//            [Fact]
//            public async Task ShouldApplyExpectedFieldFilters()
//            {
//                const string EXPECTED_FIELD_FILTERS = "webViewLink, id";
//                await instance.CreateFolderAsync(folderName, parentId);
//                createRequest.Fields.Should().Be(EXPECTED_FIELD_FILTERS);
//            }
//        }
//    }
//}
