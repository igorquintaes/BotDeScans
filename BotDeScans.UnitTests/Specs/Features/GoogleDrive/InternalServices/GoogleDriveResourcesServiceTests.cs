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

        service = fixture.Create<GoogleDriveResourcesService>();
    }

    public void Dispose()
    {
        GoogleDriveSettingsService.BaseFolderId = null!;
        GC.SuppressFinalize(this);
    }

    public class GetResourceByNameAsync : GoogleDriveResourcesServiceTests
    {
        public GetResourceByNameAsync()
        {
            fixture.Inject<List<File>>([new File()]);

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
            var expectedResult = fixture.Create<List<File>>().Single();
            var result = await service.GetResourceByNameAsync(
                fixture.Create<string>(),
                fixture.Create<string>(),
                fixture.Create<string>(),
                cancellationToken);

            result.Should().BeSuccess().And.HaveValue(expectedResult);
        }

        [Fact]
        public async Task GivenNoneFileFoundShouldReturnSuccessResultWithNullData()
        {
            var files = new List<File>();
            A.CallTo(() => fixture
                .Fake<FileList>().Files)
                .Returns(files);

            var result = await service.GetResourceByNameAsync(
                fixture.Create<string>(),
                fixture.Create<string>(),
                fixture.Create<string>(),
                cancellationToken);

            using (new AssertionScope())
            {
                result.Should().BeSuccess();
                result.Value.Should().BeNull();
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

            var result = await service.GetResourceByNameAsync(
                fixture.Create<string>(),
                fixture.Create<string>(),
                fixture.Create<string>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError("some error");
        }

        [Fact]
        public async Task GivenMultipleFilesReturnShouldReturnFailResult()
        {
            var files = new List<File> { new File(), new File() };
            A.CallTo(() => fixture
                .Fake<FileList>().Files)
                .Returns(files);

            var result = await service.GetResourceByNameAsync(
                fixture.Create<string>(),
                fixture.Create<string>(),
                fixture.Create<string>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError("Foi encontrado mais de um recurso para os dados mencionados, quando era esperado apenas um.");
        }

        [Fact]
        public async Task ShouldWriteExpectedQueryWithCustomParentId()
        {
            const int EXPECTED_PAGE_SIZE = 2;
            const string EXPECTED_QUERY_FORMAT = @"
                mimeType = '{0}'
                and name = '{1}' 
                and trashed = false
                and '{2}' in parents";

            var mimeType = fixture.Create<string>();
            var name = fixture.Create<string>();
            var parentId = fixture.Create<string>();
            var expectedQuery = string.Format(EXPECTED_QUERY_FORMAT, mimeType, name, parentId);

            await service.GetResourceByNameAsync(mimeType, name, parentId, cancellationToken);

            using (new AssertionScope())
            {
                fixture.Freeze<ListRequest>().PageSize.Should().Be(EXPECTED_PAGE_SIZE);
                fixture.Freeze<ListRequest>().Q.Should().Be(expectedQuery);
            }
        }

        [Fact]
        public async Task ShouldWriteExpectedQueryWithRootParentId()
        {
            const int EXPECTED_PAGE_SIZE = 2;
            const string EXPECTED_QUERY_FORMAT = @"
                mimeType = '{0}'
                and name = '{1}' 
                and trashed = false
                and '{2}' in parents";

            var mimeType = fixture.Create<string>();
            var name = fixture.Create<string>();
            var parentId = fixture.Create<string>();
            var expectedQuery = string.Format(EXPECTED_QUERY_FORMAT, mimeType, name, parentId);
            GoogleDriveSettingsService.BaseFolderId = parentId;

            await service.GetResourceByNameAsync(mimeType, name, null, cancellationToken);

            using (new AssertionScope())
            {
                fixture.Freeze<ListRequest>().PageSize.Should().Be(EXPECTED_PAGE_SIZE);
                fixture.Freeze<ListRequest>().Q.Should().Be(expectedQuery);
            }
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
            GoogleDriveSettingsService.BaseFolderId = fixture.Create<string>();

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
