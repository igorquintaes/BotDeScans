//using BotDeScans.App.Services;
//using BotDeScans.App.Services.Factories;
//using BotDeScans.App.Services.GoogleDrive;
//using BotDeScans.App.Wrappers;
//using FakeItEasy;
//using FluentAssertions;
//using FluentResults;
//using FluentResults.Extensions.FluentAssertions;
//using Google.Apis.Download;
//using Google.Apis.Drive.v3;
//using Google.Apis.Drive.v3.Data;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;
//using Xunit;
//using static Google.Apis.Drive.v3.FilesResource;
//using File = Google.Apis.Drive.v3.Data.File;

//namespace BotDeScans.UnitTests.Specs.Services.GoogleDrive
//{
//    public class GoogleDriveFilesServiceTests : UnitTest<GoogleDriveFilesService>
//    {
//        private readonly DriveService driveService;
//        private readonly GoogleDriveResourcesService googleDriveResourcesService;
//        private readonly GoogleDrivePermissionsService googleDrivePermissionsService;
//        private readonly FileService fileService;
//        private readonly StreamWrapper streamWrapper;
//        private readonly GoogleDriveWrapper googleDriveWrapper;
//        private readonly FilesResource filesResource;

//        public GoogleDriveFilesServiceTests()
//        {
//            var storageFactory = A.Fake<ExternalServicesFactory>();
//            driveService = A.Fake<DriveService>();
//            googleDriveResourcesService = A.Fake<GoogleDriveResourcesService>();
//            googleDrivePermissionsService = A.Fake<GoogleDrivePermissionsService>();
//            fileService = A.Fake<FileService>();
//            streamWrapper = A.Fake<StreamWrapper>();
//            googleDriveWrapper = A.Fake<GoogleDriveWrapper>();
//            filesResource = A.Fake<FilesResource>();

//            A.CallTo(() => storageFactory
//                .CreateGoogleClients())
//                .Returns(Result.Ok(driveService));

//            A.CallTo(() => driveService
//                .Files)
//                .Returns(filesResource);

//            instance = new(storageFactory,
//                           googleDriveResourcesService,
//                           googleDrivePermissionsService,
//                           fileService,
//                           streamWrapper,
//                           googleDriveWrapper);
//        }

//        public class GetFileAsync : GoogleDriveFilesServiceTests
//        {
//            private readonly string mimeType;
//            private readonly string fileName;
//            private readonly string parentId;
//            private readonly Result<File?> expectedResult;

//            public GetFileAsync()
//            {
//                mimeType = dataGenerator.Random.Word();
//                fileName = dataGenerator.Random.Word();
//                parentId = dataGenerator.Random.Word();
//                expectedResult = new Result<File?>();

//                A.CallTo(() => fileService
//                    .GetMimeType(fileName))
//                    .Returns(mimeType);

//                A.CallTo(() => googleDriveResourcesService
//                    .GetResourceByNameAsync(mimeType, fileName, parentId, cancellationToken))
//                    .Returns(expectedResult);
//            }

//            [Fact]
//            public async Task ShouldReturnFileResult()
//            {
//                var result = await instance.GetFileAsync(fileName, parentId, cancellationToken);
//                result.Should().BeSameAs(expectedResult);
//            }
//        }

//        public class GetFilesFromFolderAsync : GoogleDriveFilesServiceTests
//        {
//            private readonly ListRequest listRequest;
//            private readonly string parentId;
//            private readonly Result<FileList> expectedResponse;

//            public GetFilesFromFolderAsync()
//            {
//                listRequest = new ListRequest(null);
//                parentId = dataGenerator.Random.Word();
//                expectedResponse = new Result<FileList>();

//                A.CallTo(() => filesResource
//                    .List())
//                    .Returns(listRequest);

//                A.CallTo(() => googleDriveWrapper
//                    .ExecuteAsync(listRequest, cancellationToken))
//                    .Returns(expectedResponse);
//            }

//            [Fact]
//            public async Task ShouldReturnExpectedFileResult()
//            {
//                var result = await instance.GetFilesFromFolderAsync(parentId, cancellationToken);
//                result.Should().BeSameAs(expectedResponse);
//            }

//            [Fact]
//            public async Task ShouldApplyExpectedQueryFilters()
//            {
//                const int MAX_VALUE_PAGESIZE = 1000;
//                const string FOLDER_MIMETYPE = GoogleDriveFoldersService.FOLDER_MIMETYPE;
//                const string EXPECTED_QUERY = "trashed = false and '{0}' in parents and mimeType != '{1}'";

//                await instance.GetFilesFromFolderAsync(parentId, cancellationToken);

//                A.CallTo(() => googleDriveWrapper
//                    .ExecuteAsync(
//                        A<ListRequest>.That.Matches(listRequest => 
//                            listRequest.PageSize == MAX_VALUE_PAGESIZE &&
//                            listRequest.Q == string.Format(EXPECTED_QUERY, parentId, FOLDER_MIMETYPE)), 
//                        cancellationToken))
//                    .MustHaveHappenedOnceExactly();
//            }
//        }

//        public class UploadFileAsync : GoogleDriveFilesServiceTests
//        {
//            private readonly string mimeType;
//            private readonly string fileName;
//            private readonly string filePath;
//            private readonly string parentId;
//            private readonly CreateMediaUpload uploadRequest;
//            private readonly Result<File> expectedResponse;

//            public UploadFileAsync()
//            {
//                mimeType = dataGenerator.Random.Word();
//                filePath = Path.Combine(dataGenerator.Random.Word(), $"{dataGenerator.Random.Word()}.{dataGenerator.Lorem.Letter(3)}");
//                fileName = Path.GetFileName(filePath);
//                parentId = dataGenerator.Random.Word();
//                var file = new File();
//                var stream = A.Fake<Stream>();
//                uploadRequest = A.Fake<CreateMediaUpload>();
//                expectedResponse = Result.Ok(new File
//                {
//                    Id = dataGenerator.Random.Word()
//                });

//                A.CallTo(() => fileService
//                    .GetMimeType(filePath))
//                    .Returns(mimeType);

//                A.CallTo(() => googleDriveResourcesService
//                    .CreateResourceObject(mimeType, fileName, parentId))
//                    .Returns(file);

//                A.CallTo(() => streamWrapper
//                    .CreateFileStream(filePath, FileMode.Open))
//                    .Returns(stream);

//                A.CallTo(() => filesResource
//                    .Create(file, stream, mimeType))
//                    .Returns(uploadRequest);

//                A.CallTo(() => googleDriveWrapper
//                    .UploadAsync(uploadRequest, cancellationToken))
//                    .Returns(expectedResponse);
//            }

//            [Theory]
//            [InlineData(true)]
//            [InlineData(false)]
//            public async Task ShouldReturnExpectedResource(bool withPublicUrl)
//            {
//                var result = await instance.UploadFileAsync(filePath, parentId, withPublicUrl, cancellationToken);
//                result.Should().BeSameAs(expectedResponse);
//            }

//            [Fact]
//            public async Task ShouldUploadFileWithPublicUrl()
//            {
//                const bool WITH_PUBLIC_URL = true;
//                await instance.UploadFileAsync(filePath, parentId, WITH_PUBLIC_URL, cancellationToken);
//                A.CallTo(() => googleDrivePermissionsService
//                    .CreatePublicReaderPermissionAsync(expectedResponse.Value.Id, cancellationToken))
//                    .MustHaveHappenedOnceExactly();
//            }

//            [Fact]
//            public async Task ShouldUploadFileWithoutPublicUrl()
//            {
//                const bool WITH_PUBLIC_URL = false;
//                await instance.UploadFileAsync(filePath, parentId, WITH_PUBLIC_URL, cancellationToken);
//                A.CallTo(() => googleDrivePermissionsService
//                    .CreatePublicReaderPermissionAsync(A<string>.Ignored, A<CancellationToken>.Ignored))
//                    .MustNotHaveHappened();
//            }

//            [Theory]
//            [InlineData(true)]
//            [InlineData(false)]
//            public async Task ShouldApplyExpectedFieldFilters(bool withPublicUrl)
//            {
//                const string EXPECTED_FIELD_FILTERS = "webViewLink, id";
//                await instance.UploadFileAsync(filePath, parentId, withPublicUrl, cancellationToken);
//                uploadRequest.Fields.Should().Be(EXPECTED_FIELD_FILTERS);
//            }

//            [Fact]
//            public async Task ShouldRepassUploadAsyncError()
//            {
//                var failResult = Result.Fail("some error");

//                A.CallTo(() => googleDriveWrapper
//                    .UploadAsync(uploadRequest, cancellationToken))
//                    .Returns(failResult);

//                object result = await instance.UploadFileAsync(filePath, parentId, default, cancellationToken);
//                result.Should().BeEquivalentTo(failResult);
//            }

//            [Fact]
//            public async Task ShouldRepassCreatePublicReaderPermissionAsyncError()
//            {
//                var failResult = Result.Fail("some error");

//                A.CallTo(() => googleDrivePermissionsService
//                    .CreatePublicReaderPermissionAsync(expectedResponse.Value.Id, cancellationToken))
//                    .Returns(failResult);

//                object result = await instance.UploadFileAsync(filePath, parentId, withPublicUrl: true, cancellationToken);
//                result.Should().BeEquivalentTo(failResult);
//            }
//        }

//        public class DownloadFileAsync : GoogleDriveFilesServiceTests
//        {
//            private readonly string targetDirectory;
//            private readonly File file;
//            private readonly IDownloadProgress downloadProgress;

//            public DownloadFileAsync()
//            {
//                targetDirectory = dataGenerator.Random.Word();
//                file = new File
//                {
//                    Name = dataGenerator.Random.Word(),
//                    Id = dataGenerator.Random.Word()
//                };

//                var filePath = Path.Combine(targetDirectory, file.Name);
//                var getRequest = A.Fake<GetRequest>();
//                var stream = A.Fake<Stream>();
//                downloadProgress = A.Fake<IDownloadProgress>();

//                A.CallTo(() => filesResource
//                    .Get(file.Id))
//                    .Returns(getRequest);

//                A.CallTo(() => streamWrapper
//                    .CreateFileStream(filePath, FileMode.Create))
//                    .Returns(stream);

//                A.CallTo(() => getRequest
//                    .DownloadAsync(stream, cancellationToken))
//                    .Returns(downloadProgress);
//            }

//            [Fact]
//            public async Task ShouldReturnOkResultInASuccessfulScenario()
//            {
//                A.CallTo(() => downloadProgress
//                    .Status)
//                    .Returns(DownloadStatus.Completed);

//                var result = await instance.DownloadFileAsync(file, targetDirectory, cancellationToken);
//                result.Should().BeSuccess();
//            }

//            [Fact]
//            public async Task ShouldReturnFailResultInAnErrorScenario()
//            {
//                var exception = dataGenerator.System.Exception();
//                var failResult = new Error($"Falha ao efetuar download do arquivo {file.Name} no Google Drive.")
//                    .CausedBy(exception);

//                A.CallTo(() => downloadProgress
//                    .Status)
//                    .Returns(dataGenerator.Random.Enum<DownloadStatus>(
//                        exclude: DownloadStatus.Completed));

//                A.CallTo(() => downloadProgress
//                    .Exception)
//                    .Returns(exception);

//                var result = await instance.DownloadFileAsync(file, targetDirectory, cancellationToken);
//                result.Errors.Should().ContainEquivalentOf(failResult);
//            }
//        }
//    }
//}
