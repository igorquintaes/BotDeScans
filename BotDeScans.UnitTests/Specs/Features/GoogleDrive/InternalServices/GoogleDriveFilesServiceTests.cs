using AutoFixture;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services;
using BotDeScans.App.Services.ExternalClients;
using BotDeScans.App.Services.Wrappers;
using BotDeScans.UnitTests.Specs.Extensions;
using FakeItEasy;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static Google.Apis.Drive.v3.FilesResource;
using File = Google.Apis.Drive.v3.Data.File;
namespace BotDeScans.UnitTests.Specs.Features.GoogleDrive.InternalServices;

public class GoogleDriveFilesServiceTests : UnitTest
{
    private readonly GoogleDriveFilesService service;

    public GoogleDriveFilesServiceTests()
    {
        fixture.Fake<GoogleDriveClient>();
        fixture.Fake<GoogleDriveResourcesService>();
        fixture.Fake<GoogleDrivePermissionsService>();
        fixture.Fake<FileService>();
        fixture.Fake<StreamWrapper>();
        fixture.Fake<GoogleDriveWrapper>();

        A.CallTo(() => fixture
            .Fake<GoogleDriveClient>().Client)
            .Returns(fixture.Fake<DriveService>());

        A.CallTo(() => fixture
            .Fake<DriveService>().Files)
            .Returns(fixture.Fake<FilesResource>());

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
                .Fake<FileService>().GetMimeType(fileName))
                .Returns(mimetype);

            A.CallTo(() => fixture
                .Fake<GoogleDriveResourcesService>()
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
        public async Task GivenErrorShouldReturnFailResult()
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
                .Fake<GoogleDriveResourcesService>()
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
                .Fake<GoogleDriveResourcesService>()
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
                .Fake<FileService>()
                .GetMimeType(filePath))
                .Returns(mimeType);

            A.CallTo(() => fixture
                .Fake<GoogleDriveResourcesService>()
                .CreateResourceObject(mimeType, "file.png", parentId))
                .Returns(fixture.Fake<File>());

            A.CallTo(() => fixture
                .Fake<StreamWrapper>()
                .CreateFileStream(filePath, FileMode.Open))
                .Returns(fixture.Fake<Stream>());

            A.CallTo(() => fixture
                .Fake<FilesResource>()
                .Create(fixture.Fake<File>(), fixture.Fake<Stream>(), mimeType))
                .Returns(fixture.Fake<CreateMediaUpload>());

            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .UploadAsync(fixture.Fake<CreateMediaUpload>(), cancellationToken))
                .Returns(fixture.Fake<File>());
        }

        [Fact]
        public async Task GivenExecutionShouldFillUploadRequestFields()
        {
            await service.UploadAsync(filePath, parentId, withPublicUrl: true, cancellationToken);

            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .UploadAsync(
                    A<CreateMediaUpload>.That.Matches(x => x.Fields == "webViewLink, id"),
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessExecutionWithPublicUrlShouldReturnSuccessResult()
        {
            var result = await service.UploadAsync(filePath, parentId, withPublicUrl: true, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Fake<File>());

            A.CallTo(() => fixture
                .Fake<GoogleDrivePermissionsService>()
                .CreatePublicReaderPermissionAsync(fixture.Fake<File>().Id, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessExecutionWithoutPublicUrlShouldReturnSuccessResult()
        {
            var result = await service.UploadAsync(filePath, parentId, withPublicUrl: false, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Fake<File>());

            A.CallTo(() => fixture
                .Fake<GoogleDrivePermissionsService>()
                .CreatePublicReaderPermissionAsync(A<string>.Ignored, cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenErrorToUploadFileShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .UploadAsync(fixture.Fake<CreateMediaUpload>(), cancellationToken))
                .Returns(Result.Fail("some error"));

            var result = await service.UploadAsync(filePath, parentId, default, cancellationToken);

            result.Should().BeFailure().And.HaveError("some error");
        }

        [Fact]
        public async Task GivenErrorToCreatePublicReaderPermissionShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .Fake<GoogleDrivePermissionsService>()
                .CreatePublicReaderPermissionAsync(fixture.Fake<File>().Id, cancellationToken))
                .Returns(Result.Fail("some error"));

            var result = await service.UploadAsync(filePath, parentId, withPublicUrl: true, cancellationToken);

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
                .Fake<FileService>()
                .GetMimeType(filePath))
                .Returns(mimeType);

            A.CallTo(() => fixture
                .Fake<StreamWrapper>()
                .CreateFileStream(filePath, FileMode.Open))
                .Returns(fixture.Fake<Stream>());

            A.CallTo(() => fixture
                .Fake<FilesResource>()
                .Update(A<File>.Ignored, oldFileId, fixture.Fake<Stream>(), mimeType))
                .Returns(fixture.Fake<UpdateMediaUpload>());

            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .UploadAsync(fixture.Fake<UpdateMediaUpload>(), cancellationToken))
                .Returns(fixture.Fake<File>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await service.UpdateAsync(filePath, oldFileId, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Fake<File>());
        }

        [Fact]
        public async Task GivenErrorToUpdateShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
                .UploadAsync(fixture.Fake<UpdateMediaUpload>(), cancellationToken))
                .Returns(Result.Fail("some error"));

            var result = await service.UpdateAsync(filePath, oldFileId, cancellationToken);

            result.Should().BeFailure().And.HaveError("some error");
        }

        [Fact]
        public async Task GivenExecutionShouldFillUpdateRequestFields()
        {
            var result = await service.UpdateAsync(filePath, oldFileId, cancellationToken);

            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>()
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
                .Fake<FilesResource>()
                .Get(file.Id))
                .Returns(fixture.Fake<GetRequest>());

            A.CallTo(() => fixture
                .Fake<StreamWrapper>()
                .CreateFileStream(filePath, FileMode.Create))
                .Returns(fixture.Fake<Stream>());

            A.CallTo(() => fixture
                .Fake<GetRequest>()
                .DownloadAsync(fixture.Fake<Stream>(), cancellationToken))
                .Returns(fixture.Fake<IDownloadProgress>());

            A.CallTo(() => fixture
                .Fake<IDownloadProgress>().Status)
                .Returns(DownloadStatus.Completed);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnOkResult()
        {
            var result = await service.DownloadAsync(file, targetDirectory, cancellationToken);

            result.Should().BeSuccess();
        }

        [Theory]
        [InlineData(DownloadStatus.Failed)]
        [InlineData(DownloadStatus.Downloading)]
        [InlineData(DownloadStatus.NotStarted)]
        public async Task GivenErrorExecutionShouldReturnFailResult(DownloadStatus downloadStatus)
        {
            var exception = new Exception("some exception error");
            A.CallTo(() => fixture
                .Fake<IDownloadProgress>().Status)
                .Returns(downloadStatus);

            A.CallTo(() => fixture
                .Fake<IDownloadProgress>().Exception)
                .Returns(exception);

            var result = await service.DownloadAsync(file, targetDirectory, cancellationToken);

            using var _ = new AssertionScope();
            result.Should().BeFailure();
            result.Should().HaveError($"Falha ao efetuar download do arquivo {file.Name} no Google Drive.");
            result.Errors.Should().HaveCount(1);
            result.Errors.FirstOrDefault()?.Reasons.Should().HaveCount(1);
            result.Errors.FirstOrDefault()?.Reasons.FirstOrDefault()?.Should().BeOfType<ExceptionalError>();
            result.Errors.FirstOrDefault()?.Reasons.FirstOrDefault(x => x is ExceptionalError)?
                  .As<ExceptionalError>().Exception.Should().Be(exception);
        }
    }
}