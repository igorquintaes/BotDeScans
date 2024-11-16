using BotDeScans;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services.ExternalClients;
using BotDeScans.App.Services.Wrappers;
using BotDeScans.UnitTests;
using BotDeScans.UnitTests.Specs;
using BotDeScans.UnitTests.Specs.Features;
using BotDeScans.UnitTests.Specs.Features.GoogleDrive;
using BotDeScans.UnitTests.Specs.Features.GoogleDrive.InternalServices;
using BotDeScans.UnitTests.Specs.Services;
using CG.Web.MegaApiClient;
using FakeItEasy;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using Google.Apis.Blogger.v3;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static Google.Apis.Drive.v3.FilesResource;
namespace BotDeScans.UnitTests.Specs.Features.GoogleDrive.InternalServices;

public class GoogleDriveResourcesServiceTests : UnitTest
{
    private readonly GoogleDriveResourcesService service;
    private readonly DriveService driveService;
    private readonly GoogleDriveWrapper googleDriveWrapper;

    public GoogleDriveResourcesServiceTests()
    {
        driveService = A.Fake<DriveService>();
        googleDriveWrapper = A.Fake<GoogleDriveWrapper>();
        var filesResource = A.Fake<FilesResource>();

        A.CallTo(() => driveService
            .Files)
            .Returns(filesResource);

        var client = A.Fake<GoogleDriveClient>();
        A.CallTo(() => client.Client).Returns(driveService);

        service = new(client, googleDriveWrapper);
    }

    public class GetResourceByNameAsync : GoogleDriveResourcesServiceTests
    {
        private readonly ListRequest listRequest;
        private readonly FileList filesList;
        private readonly List<File> files;

        public GetResourceByNameAsync()
        {
            listRequest = A.Fake<ListRequest>();
            filesList = A.Fake<FileList>();
            files = new List<File> { new File() };

            A.CallTo(() => driveService.Files
                .List())
                .Returns(listRequest);

            A.CallTo(() => googleDriveWrapper
                .ExecuteAsync(listRequest, cancellationToken))
                .Returns(filesList);

            A.CallTo(() => filesList
                .Files)
                .Returns(files);
        }

        [Fact]
        public async Task ShouldGetExpectedResource()
        {
            var expectedResult = files.Single();
            var result = await service.GetResourceByNameAsync(
                dataGenerator.Random.Word(),
                dataGenerator.Random.Word(),
                dataGenerator.Random.Word(),
                cancellationToken);

            result.Should().BeSuccess().And.HaveValue(expectedResult);
        }

        [Fact]
        public async Task ShouldReturnNullIfFileIsNotFound()
        {
            var files = new List<File>();
            A.CallTo(() => filesList
                .Files)
                .Returns(files);

            var result = await service.GetResourceByNameAsync(
                dataGenerator.Random.Word(),
                dataGenerator.Random.Word(),
                dataGenerator.Random.Word(),
                cancellationToken);

            // TODO: Waiting https://github.com/altmann/FluentResults/issues/172
            //result.Should().BeSuccess().And.HaveValue(null);

            using (new AssertionScope())
            {
                result.Should().BeSuccess();
                result.Value.Should().BeNull();
            }
        }

        [Fact]
        public async Task ShouldRepassExecuteAsyncListRequestError()
        {
            var errorResult = Result.Fail("some error");

            A.CallTo(() => googleDriveWrapper
                .ExecuteAsync(listRequest, cancellationToken))
                .Returns(errorResult);

            object result = await service.GetResourceByNameAsync(
                dataGenerator.Random.Word(),
                dataGenerator.Random.Word(),
                dataGenerator.Random.Word(),
                cancellationToken);

            result.Should().BeEquivalentTo(errorResult);
        }

        [Fact]
        public async Task ShouldReturnErrorIfMoreThanOneFileIsFound()
        {
            var files = new List<File> { new File(), new File() };
            A.CallTo(() => filesList
                .Files)
                .Returns(files);

            var result = await service.GetResourceByNameAsync(
                dataGenerator.Random.Word(),
                dataGenerator.Random.Word(),
                dataGenerator.Random.Word(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError("Foi encontrado mais de um recurso para os dados mencionados, quando era esperado apenas um.");
        }

        [Fact]
        public async Task ShouldApplyExpectedQueryFilters()
        {
            var mimeType = dataGenerator.Random.Word();
            var name = dataGenerator.Random.Word();
            var parentId = dataGenerator.Random.Word();
            const int EXPECTED_PAGE_SIZE = 2;
            const string EXPECTED_QUERY = @"
                mimeType = '{0}'
                and name = '{1}' 
                and trashed = false
                and '{2}' in parents";

            await service.GetResourceByNameAsync(mimeType, name, parentId, cancellationToken);

            using (new AssertionScope())
            {
                listRequest.PageSize.Should().Be(EXPECTED_PAGE_SIZE);
                listRequest.Q.Should().Be(string.Format(EXPECTED_QUERY, mimeType, name, parentId));
            }
        }

        [Fact]
        public async Task ShouldQueryRootFolderAsParentIdIfParameterValueBeNull()
        {
            var mimeType = dataGenerator.Random.Word();
            var name = dataGenerator.Random.Word();
            var parentId = dataGenerator.Random.Word();
            GoogleDriveSettingsService.BaseFolderId = parentId;

            const string EXPECTED_QUERY = @"
                mimeType = '{0}'
                and name = '{1}' 
                and trashed = false
                and '{2}' in parents";

            await service.GetResourceByNameAsync(mimeType, name, null, cancellationToken);
            listRequest.Q.Should().Be(string.Format(EXPECTED_QUERY, mimeType, name, parentId));
        }
    }

    public class CreateResourceObject : GoogleDriveResourcesServiceTests
    {
        [Fact]
        public void ShouldCreateResourceObjectWithDefinedParentId()
        {
            var mimeType = dataGenerator.Random.Word();
            var name = dataGenerator.Random.Word();
            var parentId = dataGenerator.Random.Word();
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

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void ShouldCreateResourceObjectRootAsParentId(string parentId)
        {
            var mimeType = dataGenerator.Random.Word();
            var name = dataGenerator.Random.Word();
            GoogleDriveSettingsService.BaseFolderId = dataGenerator.Random.Word();

            var expectedResult = new
            {
                Name = name,
                Description = name,
                MimeType = mimeType,
                Parents = new[] { GoogleDriveSettingsService.BaseFolderId }
            };

            var result = service.CreateResourceObject(mimeType, name, parentId);
            result.Should().BeEquivalentTo(expectedResult);
        }
    }

    public class DeleteResource : GoogleDriveResourcesServiceTests
    {
        [Fact]
        public async Task ShouldDeleteAsExpected()
        {
            var resourceId = dataGenerator.Random.Word();
            var deleteRequest = A.Fake<DeleteRequest>();
            var expectedResult = new Result<string>();

            A.CallTo(() => driveService.Files
                .Delete(resourceId))
                .Returns(deleteRequest);

            A.CallTo(() => googleDriveWrapper
                .ExecuteAsync(deleteRequest, cancellationToken))
                .Returns(expectedResult);

            var result = await service.DeleteResource(resourceId, cancellationToken);
            result.Should().BeSameAs(expectedResult);
        }
    }
}
