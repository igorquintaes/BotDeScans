using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services.Wrappers;
using FluentAssertions.Execution;
using FluentResults;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using static Google.Apis.Drive.v3.FilesResource;
using File = Google.Apis.Drive.v3.Data.File;
namespace BotDeScans.UnitTests.Specs.Features.GoogleDrive.InternalServices;

public class GoogleDriveResourcesServiceTests : UnitTest, IDisposable
{
    private readonly GoogleDriveResourcesService service;

    public GoogleDriveResourcesServiceTests()
    {
        fixture.FreezeFake<DriveService>();
        fixture.FreezeFake<GoogleWrapper>();

        A.CallTo(() => fixture
            .FreezeFake<DriveService>().Files)
            .Returns(fixture.FreezeFake<FilesResource>());

        GoogleDriveSettingsService.BaseFolderId = fixture.Create<string>();

        service = fixture.Create<GoogleDriveResourcesService>();
    }

    public void Dispose()
    {
        GoogleDriveSettingsService.BaseFolderId = null!;
        GC.SuppressFinalize(this);
    }

    public class GetResourcesAsync : GoogleDriveResourcesServiceTests
    {
        public GetResourcesAsync()
        {
            fixture.Inject<List<File>>([new File(), new File()]);

            A.CallTo(() => fixture
                .FreezeFake<FilesResource>().List())
                .Returns(fixture.FreezeFake<ListRequest>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>().ExecuteAsync(
                    fixture.FreezeFake<ListRequest>(),
                    cancellationToken))
                .Returns(fixture.FreezeFake<FileList>());

            A.CallTo(() => fixture
                .FreezeFake<FileList>().Files)
                .Returns(fixture.Create<List<File>>());
        }

        [Fact]
        public async Task GivenExecutionShouldReturnSuccessResultWithExpectedData()
        {
            var expectedResult = fixture.Create<List<File>>();
            var result = await service.GetResourcesAsync(
                mimeType: default,
                forbiddenMimeType: default,
                name: default,
                parentId: default,
                minResult: default,
                maxResult: default,
                cancellationToken);

            result.Should().BeSuccess().And.HaveValue(expectedResult);
        }

        [Fact]
        public async Task GivenNoneFileFoundShouldReturnSuccessResultWithEmptyData()
        {
            var files = new List<File>();
            A.CallTo(() => fixture
                .FreezeFake<FileList>().Files)
                .Returns(files);

            var result = await service.GetResourcesAsync(
                mimeType: default,
                forbiddenMimeType: default,
                name: default,
                parentId: default,
                minResult: default,
                maxResult: default,
                cancellationToken);

            using (new AssertionScope())
            {
                result.Should().BeSuccess();
                result.Value.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task GivenExecutionErrorShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>().ExecuteAsync(
                    fixture.FreezeFake<ListRequest>(),
                    cancellationToken))
                .Returns(Result.Fail("some error"));

            var result = await service.GetResourcesAsync(
                mimeType: default,
                forbiddenMimeType: default,
                name: default,
                parentId: default,
                minResult: default,
                maxResult: default,
                cancellationToken);

            result.Should().BeFailure().And.HaveError("some error");
        }

        [Fact]
        public async Task GivenMoreThanMaxExpectedFilesShouldReturnFailResult()
        {
            const int maxResult = 1;
            var files = new List<File> { new(), new() };
            A.CallTo(() => fixture
                .FreezeFake<FileList>().Files)
                .Returns(files);

            var result = await service.GetResourcesAsync(
                mimeType: default,
                forbiddenMimeType: default,
                name: default,
                parentId: default,
                minResult: default,
                maxResult: maxResult,
                cancellationToken);

            result.Should().BeFailure().And.HaveError($"Foi encontrado mais de um recurso para os dados mencionados, quando era esperado no máximo {maxResult}.");
        }

        [Fact]
        public async Task GivenLessThanMinExpectedFilesShouldReturnFailResult()
        {
            const int minResult = 2;
            var files = new List<File> { new() };
            A.CallTo(() => fixture
                .FreezeFake<FileList>().Files)
                .Returns(files);

            var result = await service.GetResourcesAsync(
                mimeType: default,
                forbiddenMimeType: default,
                name: default,
                parentId: default,
                minResult: minResult,
                maxResult: default,
                cancellationToken);

            result.Should().BeFailure().And.HaveError($"Foi encontrado mais de um recurso para os dados mencionados, quando era esperado no mínimo {minResult}.");
        }

        [Fact]
        public async Task ShouldWriteExpectedQueryWithCustomParentId()
        {
            const string EXPECTED_QUERY_FORMAT = @"trashed = false and '{0}' in parents";

            var parentId = fixture.Create<string>();
            var expectedQuery = string.Format(EXPECTED_QUERY_FORMAT, parentId);

            await service.GetResourcesAsync(
                mimeType: default,
                forbiddenMimeType: default,
                name: default,
                parentId,
                minResult: default,
                maxResult: default,
                cancellationToken);

            fixture.Freeze<ListRequest>().Q.Should().Be(expectedQuery);
        }

        [Fact]
        public async Task ShouldWriteExpectedQueryWithRootParentId()
        {
            const string EXPECTED_QUERY_FORMAT = @"trashed = false and '{0}' in parents";

            var expectedQuery = string.Format(
                EXPECTED_QUERY_FORMAT,
                GoogleDriveSettingsService.BaseFolderId);

            await service.GetResourcesAsync(
                mimeType: default,
                forbiddenMimeType: default,
                name: default,
                parentId: default,
                minResult: default,
                maxResult: default,
                cancellationToken);

            fixture.Freeze<ListRequest>().Q.Should().Be(expectedQuery);
        }

        [Fact]
        public async Task ShouldWriteExpectedQueryWithResourceName()
        {
            const string EXPECTED_QUERY_FORMAT = @"trashed = false and name = '{0}' and '{1}' in parents";

            var name = fixture.Create<string>();
            var expectedQuery = string.Format(
                EXPECTED_QUERY_FORMAT,
                name,
                GoogleDriveSettingsService.BaseFolderId);

            await service.GetResourcesAsync(
                mimeType: default,
                forbiddenMimeType: default,
                name: name,
                parentId: default,
                minResult: default,
                maxResult: default,
                cancellationToken);

            fixture.Freeze<ListRequest>().Q.Should().Be(expectedQuery);
        }

        [Fact]
        public async Task ShouldWriteExpectedQueryWithMimeType()
        {
            const string EXPECTED_QUERY_FORMAT = @"trashed = false and mimeType = '{0}' and '{1}' in parents";

            var mimeType = fixture.Create<string>();
            var expectedQuery = string.Format(
                EXPECTED_QUERY_FORMAT,
                mimeType,
                GoogleDriveSettingsService.BaseFolderId);

            await service.GetResourcesAsync(
                mimeType: mimeType,
                forbiddenMimeType: default,
                name: default,
                parentId: default,
                minResult: default,
                maxResult: default,
                cancellationToken);

            fixture.Freeze<ListRequest>().Q.Should().Be(expectedQuery);
        }

        [Fact]
        public async Task ShouldWriteExpectedQueryWithForbiddenMimeType()
        {
            const string EXPECTED_QUERY_FORMAT = @"trashed = false and mimeType != '{0}' and '{1}' in parents";

            var forbiddenMimeType = fixture.Create<string>();
            var expectedQuery = string.Format(
                EXPECTED_QUERY_FORMAT,
                forbiddenMimeType,
                GoogleDriveSettingsService.BaseFolderId);

            await service.GetResourcesAsync(
                mimeType: default,
                forbiddenMimeType: forbiddenMimeType,
                name: default,
                parentId: default,
                minResult: default,
                maxResult: default,
                cancellationToken);

            fixture.Freeze<ListRequest>().Q.Should().Be(expectedQuery);
        }

        [Fact]
        public async Task ShouldWriteExpectedQueryWithAllFilledFields()
        {
            const string EXPECTED_QUERY_FORMAT = @"trashed = false and mimeType = '{0}' and mimeType != '{1}' and name = '{2}' and '{3}' in parents";

            var mimeType = fixture.Create<string>();
            var forbiddenMimeType = fixture.Create<string>();
            var name = fixture.Create<string>();
            var parentId = fixture.Create<string>();
            var expectedQuery = string.Format(
                EXPECTED_QUERY_FORMAT,
                mimeType,
                forbiddenMimeType,
                name,
                parentId);

            await service.GetResourcesAsync(
                mimeType: mimeType,
                forbiddenMimeType: forbiddenMimeType,
                name: name,
                parentId: parentId,
                minResult: default,
                maxResult: default,
                cancellationToken);

            fixture.Freeze<ListRequest>().Q.Should().Be(expectedQuery);
        }
    }

    public class CreateResourceObject : GoogleDriveResourcesServiceTests
    {
        [Fact]
        public void GivenDataWithParentIdShouldCreateExpectedResourceObject()
        {
            var mimeType = fixture.Create<string>();
            var name = fixture.Create<string>();
            var parentId = fixture.Create<string>();
            var expectedResult = new
            {
                Name = name,
                Description = name,
                MimeType = mimeType,
                Parents = new[] { parentId }
            };

            var result = service.CreateResourceObject(mimeType, name, parentId);
            result.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void GivenDataWithoutParentIdShouldCreateExpectedResourceObject()
        {
            var mimeType = fixture.Create<string>();
            var name = fixture.Create<string>();

            var expectedResult = new
            {
                Name = name,
                Description = name,
                MimeType = mimeType,
                Parents = new[] { GoogleDriveSettingsService.BaseFolderId }
            };

            var result = service.CreateResourceObject(mimeType, name);
            result.Should().BeEquivalentTo(expectedResult);
        }
    }

    public class DeleteResource : GoogleDriveResourcesServiceTests
    {
        [Fact]
        public async Task GivenValidExecutionShouldReturnSuccessResultWithResourceIdData()
        {
            var resourceId = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<FilesResource>().Delete(resourceId))
                .Returns(fixture.Freeze<DeleteRequest>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>().ExecuteAsync(fixture.FreezeFake<DeleteRequest>(), cancellationToken))
                .Returns(Result.Ok("delete-id"));

            var result = await service.DeleteResource(resourceId, cancellationToken);
            result.Should().BeSuccess().And.HaveValue("delete-id");
        }

        [Fact]
        public async Task GivenValidExecutionShouldReturnFailResult()
        {
            var resourceId = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<FilesResource>().Delete(resourceId))
                .Returns(fixture.FreezeFake<DeleteRequest>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>().ExecuteAsync(fixture.FreezeFake<DeleteRequest>(), cancellationToken))
                .Returns(Result.Fail("some error"));

            var result = await service.DeleteResource(resourceId, cancellationToken);
            result.Should().BeFailure().And.HaveError("some error");
        }
    }
}
