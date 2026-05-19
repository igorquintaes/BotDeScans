using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services;
using BotDeScans.App.Services.Wrappers;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentResults;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using static Google.Apis.Drive.v3.FilesResource;
using File = Google.Apis.Drive.v3.Data.File;

namespace BotDeScans.UnitTests.Specs.Features.GoogleDrive.InternalServices;

public class GoogleDriveFilesServiceTests : UnitTest
{
    private readonly GoogleDriveFilesService service;

    public GoogleDriveFilesServiceTests()
    {
        fixture.FreezeFake<DriveService>();
        fixture.FreezeFake<GoogleDriveResourcesService>();
        fixture.FreezeFake<GoogleDrivePermissionsService>();
        fixture.FreezeFake<FileService>();
        fixture.FreezeFake<StreamWrapper>();
        fixture.FreezeFake<GoogleWrapper>();

        A.CallTo(() => fixture
            .FreezeFake<DriveService>().Files)
            .Returns(fixture.FreezeFake<FilesResource>());

        service = fixture.Create<GoogleDriveFilesService>();
    }

    public class GetAsync : GoogleDriveFilesServiceTests
    {
        [Fact]
        public async Task GivenSuccessExecutionAndFoundFileShouldReturnSuccessResultWithData()
        {
            var fileName = fixture.Create<string>();
            var mimetype = fixture.Create<string>();
            var parentId = fixture.Create<string>();
            var expectedResult = new List<File>() { new() };

            A.CallTo(() => fixture
                .FreezeFake<FileService>().GetMimeType(fileName))
                .Returns(mimetype);

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveResourcesService>()
                .GetResourcesAsync(
                    mimetype,
                    default,
                    fileName,
                    parentId,
                    default,
                    1,
                    cancellationToken))
                .Returns(expectedResult);

            var result = await service.GetAsync(fileName, parentId, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(expectedResult[0]);
        }

        [Fact]
        public async Task GivenSuccessExecutionAndNotFoundFileShouldReturnSuccessResultWithNullData()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveResourcesService>()
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
        public async Task GivenErrorShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveResourcesService>()
                .GetResourcesAsync(
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    A<int?>.Ignored,
                    A<int?>.Ignored,
                    cancellationToken))
                .Returns(Result.Fail("some error"));

            var result = await service.GetAsync(
                fixture.Create<string>(),
                fixture.Create<string>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError("some error");
        }
    }

    public class GetManyAsync : GoogleDriveFilesServiceTests
    {
        [Fact]
        public async Task GivenSuccessExecutionShouldReturnSuccessResultWithData()
        {
            const string FOLDER_MIMETYPE = "application/vnd.google-apps.folder";
            var parentId = fixture.Create<string>();
            var expectedResult = new List<File>() { new() };

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveResourcesService>()
                .GetResourcesAsync(
                    default,
                    FOLDER_MIMETYPE,
                    default,
                    parentId,
                    default,
                    default,
                    cancellationToken))
                .Returns(expectedResult);

            var result = await service.GetManyAsync(parentId, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(expectedResult);
        }

        [Fact]
        public async Task GivenErrorShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveResourcesService>()
                .GetResourcesAsync(
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    A<int?>.Ignored,
                    A<int?>.Ignored,
                    cancellationToken))
                .Returns(Result.Fail("some error"));

            var result = await service.GetManyAsync(
                fixture.Create<string>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError("some error");
        }
    }

    public class UploadAsync : GoogleDriveFilesServiceTests
    {
        private readonly string filePath;
        private readonly string parentId;

        public UploadAsync()
        {
            filePath = Path.Combine("directory", "file.png");
            parentId = fixture.Create<string>();
            var mimeType = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<FileService>()
                .GetMimeType(filePath))
                .Returns(mimeType);

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveResourcesService>()
                .CreateResourceObject(mimeType, "file.png", parentId))
                .Returns(fixture.FreezeFake<File>());

            A.CallTo(() => fixture
                .FreezeFake<StreamWrapper>()
                .CreateFileStream(filePath, FileMode.Open))
                .Returns(fixture.FreezeFake<Stream>());

            A.CallTo(() => fixture
                .FreezeFake<FilesResource>()
                .Create(fixture.FreezeFake<File>(), fixture.FreezeFake<Stream>(), mimeType))
                .Returns(fixture.FreezeFake<CreateMediaUpload>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>()
                .UploadAsync(fixture.FreezeFake<CreateMediaUpload>(), cancellationToken))
                .Returns(fixture.FreezeFake<File>());
        }

        [Fact]
        public async Task GivenExecutionShouldFillUploadRequestFields()
        {
            await service.UploadAsync(filePath, parentId, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>()
                .UploadAsync(
                    A<CreateMediaUpload>.That.Matches(x => x.Fields == "webViewLink, id"),
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorToUploadFileShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>()
                .UploadAsync(fixture.FreezeFake<CreateMediaUpload>(), cancellationToken))
                .Returns(Result.Fail("some error"));

            var result = await service.UploadAsync(filePath, parentId, cancellationToken);

            result.Should().BeFailure().And.HaveError("some error");
        }
    }

    public class UpdateAsync : GoogleDriveFilesServiceTests
    {
        private readonly string filePath;
        private readonly string oldFileId;

        public UpdateAsync()
        {
            filePath = Path.Combine("directory", "file.png");
            oldFileId = fixture.Create<string>();
            var mimeType = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<FileService>()
                .GetMimeType(filePath))
                .Returns(mimeType);

            A.CallTo(() => fixture
                .FreezeFake<StreamWrapper>()
                .CreateFileStream(filePath, FileMode.Open))
                .Returns(fixture.FreezeFake<Stream>());

            A.CallTo(() => fixture
                .FreezeFake<FilesResource>()
                .Update(A<File>.Ignored, oldFileId, fixture.FreezeFake<Stream>(), mimeType))
                .Returns(fixture.FreezeFake<UpdateMediaUpload>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>()
                .UploadAsync(fixture.FreezeFake<UpdateMediaUpload>(), cancellationToken))
                .Returns(fixture.FreezeFake<File>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await service.UpdateAsync(filePath, oldFileId, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.FreezeFake<File>());
        }

        [Fact]
        public async Task GivenErrorToUpdateShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>()
                .UploadAsync(fixture.FreezeFake<UpdateMediaUpload>(), cancellationToken))
                .Returns(Result.Fail("some error"));

            var result = await service.UpdateAsync(filePath, oldFileId, cancellationToken);

            result.Should().BeFailure().And.HaveError("some error");
        }

        [Fact]
        public async Task GivenExecutionShouldFillUpdateRequestFields()
        {
            var result = await service.UpdateAsync(filePath, oldFileId, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>()
                .UploadAsync(
                    A<UpdateMediaUpload>.That.Matches(x => x.Fields == "webViewLink, id"),
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }
    }

    public class DownloadAsync : GoogleDriveFilesServiceTests
    {
        private readonly File file;
        private readonly string targetDirectory;

        public DownloadAsync()
        {
            file = fixture.Create<File>();
            targetDirectory = fixture.Create<string>();
            var filePath = Path.Combine(targetDirectory, file.Name);

            A.CallTo(() => fixture
                .FreezeFake<FilesResource>()
                .Get(file.Id))
                .Returns(fixture.FreezeFake<GetRequest>());

            A.CallTo(() => fixture
                .FreezeFake<StreamWrapper>()
                .CreateFileStream(filePath, FileMode.Create))
                .Returns(fixture.FreezeFake<Stream>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>()
                .ExecuteAsync(A<Func<Task<IDownloadProgress>>>._, cancellationToken))
                .Returns(Result.Ok(fixture.FreezeFake<IDownloadProgress>()));

            A.CallTo(() => fixture
                .FreezeFake<IDownloadProgress>().Status)
                .Returns(DownloadStatus.Completed);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnOkResult()
        {
            var reason = string.Format(GoogleDriveFilesService.DOWNLOAD_STATUS, DownloadStatus.Completed);

            var result = await service.DownloadAsync(file, targetDirectory, cancellationToken);

            result.Should().BeSuccess().And
                  .HaveReason(reason);
        }

        [Theory]
        [InlineData(DownloadStatus.Failed)]
        [InlineData(DownloadStatus.Downloading)]
        [InlineData(DownloadStatus.NotStarted)]
        public async Task GivenErrorExecutionShouldReturnFailResult(DownloadStatus downloadStatus)
        {
            var reason = string.Format(GoogleDriveFilesService.DOWNLOAD_STATUS, downloadStatus);

            A.CallTo(() => fixture
                .FreezeFake<IDownloadProgress>().Status)
                .Returns(downloadStatus);

            var result = await service.DownloadAsync(file, targetDirectory, cancellationToken);

            result.Should().BeFailure().And
                  .HaveError(reason);
        }

        [Fact]
        public async Task GivenFailDownloadExecutionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";
            var reason = string.Format(GoogleDriveFilesService.DOWNLOAD_STATUS, DownloadStatus.Failed);

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>()
                .ExecuteAsync(A<Func<Task<IDownloadProgress>>>._, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.DownloadAsync(file, targetDirectory, cancellationToken);

            result.Should().BeFailure().And
                  .HaveError(ERROR_MESSAGE).And
                  .HaveError(reason);
        }

        [Fact]
        public async Task GivenExceptionExecutionShouldReturnFailResult()
        {
            var reason = string.Format(GoogleDriveFilesService.DOWNLOAD_STATUS, DownloadStatus.Failed);
            var exception = new InvalidOperationException("some error.");

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>()
                .ExecuteAsync(A<Func<Task<IDownloadProgress>>>._, cancellationToken))
                .Returns(Result.Ok(fixture.FreezeFake<IDownloadProgress>()));

            A.CallTo(() => fixture
                .FreezeFake<IDownloadProgress>().Status)
                .Returns(DownloadStatus.Failed);

            A.CallTo(() => fixture
                .FreezeFake<IDownloadProgress>().Exception)
                .Returns(exception);

            var result = await service.DownloadAsync(file, targetDirectory, cancellationToken);

            result.Should().BeFailure();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].Message.Should().Be(reason);
            result.Errors[0].Reasons.Should().HaveCount(1);
            result.Errors[0].Reasons[0].Should().BeOfType<ExceptionalError>();
            result.Errors[0].Reasons[0].As<ExceptionalError>().Exception.Should().Be(exception);
        }

        [Fact]
        public async Task GivenSuccessShouldReturnDownloadReasons()
        {
            const string SUCCESS_MESSAGE = "success.";
            var reason = string.Format(GoogleDriveFilesService.DOWNLOAD_STATUS, DownloadStatus.Completed);

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>()
                .ExecuteAsync(A<Func<Task<IDownloadProgress>>>._, cancellationToken))
                .Returns(Result.Ok(fixture
                    .FreezeFake<IDownloadProgress>())
                    .WithSuccess(SUCCESS_MESSAGE));

            var result = await service.DownloadAsync(file, targetDirectory, cancellationToken);

            result.Should().BeSuccess().And.HaveReason(reason);
            result.Should().BeSuccess().And.HaveReason(SUCCESS_MESSAGE);
        }
    }
}