using AutoFixture;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static Google.Apis.Drive.v3.FilesResource;

namespace BotDeScans.UnitTests.Specs.Features.GoogleDrive.InternalServices;

public class GoogleDriveResourcesServiceTests : UnitTest,  IDisposable
{
    private readonly GoogleDriveResourcesService service;

    public GoogleDriveResourcesServiceTests()
    {
        fixture.Fake<GoogleDriveClient>();
        fixture.Fake<GoogleDriveWrapper>();

        A.CallTo(() => fixture
            .Fake<GoogleDriveClient>().Client)
            .Returns(fixture.Fake<DriveService>());

        A.CallTo(() => fixture
            .Fake<DriveService>().Files)
            .Returns(fixture.Fake<FilesResource>());

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
                .Fake<FilesResource>().List())
                .Returns(fixture.Fake<ListRequest>());

            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>().ExecuteAsync(
                    fixture.Fake<ListRequest>(), 
                    cancellationToken))
                .Returns(fixture.Fake<FileList>());

            A.CallTo(() => fixture
                .Fake<FileList>().Files)
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
                .Fake<FileList>().Files)
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
                .Fake<GoogleDriveWrapper>().ExecuteAsync(
                    fixture.Fake<ListRequest>(),
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
            var files = new List<File> { new File(), new File() };
            A.CallTo(() => fixture
                .Fake<FileList>().Files)
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
            var files = new List<File> { new File() };
            A.CallTo(() => fixture
                .Fake<FileList>().Files)
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
            const string EXPECTED_QUERY_FORMAT = @"trashed = false and '{1}' in parents";

            var mimeType = fixture.Create<string>();
            var parentId = fixture.Create<string>();
            var expectedQuery = string.Format(EXPECTED_QUERY_FORMAT, mimeType, parentId);

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
            const string EXPECTED_QUERY_FORMAT = @"trashed = false and '{1}' in parents";

            var mimeType = fixture.Create<string>();
            var parentId = fixture.Create<string>();
            var expectedQuery = string.Format(
                EXPECTED_QUERY_FORMAT, 
                mimeType, 
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
            const string EXPECTED_QUERY_FORMAT = @"trashed = false and name = '{1}' and '{2}' in parents";

            var mimeType = fixture.Create<string>();
            var name = fixture.Create<string>();
            var parentId = fixture.Create<string>();
            var expectedQuery = string.Format(
                EXPECTED_QUERY_FORMAT, 
                mimeType, 
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
                .Fake<FilesResource>().Delete(resourceId))
                .Returns(fixture.Freeze<DeleteRequest>());

            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>().ExecuteAsync(fixture.Fake<DeleteRequest>(), cancellationToken))
                .Returns(Result.Ok("delete-id"));

            var result = await service.DeleteResource(resourceId, cancellationToken);
            result.Should().BeSuccess().And.HaveValue("delete-id");
        }

        [Fact]
        public async Task GivenValidExecutionShouldReturnFailResult()
        {
            var resourceId = fixture.Create<string>();

            A.CallTo(() => fixture
                .Fake<FilesResource>().Delete(resourceId))
                .Returns(fixture.Fake<DeleteRequest>());

            A.CallTo(() => fixture
                .Fake<GoogleDriveWrapper>().ExecuteAsync(fixture.Fake<DeleteRequest>(), cancellationToken))
                .Returns(Result.Fail("some error"));

            var result = await service.DeleteResource(resourceId, cancellationToken);
            result.Should().BeFailure().And.HaveError("some error");
        }
    }
}
