using AutoFixture;
using BotDeScans.App.Features.Mega.InternalServices;
using BotDeScans.App.Services.ExternalClients;
using BotDeScans.App.Services.Wrappers;
using BotDeScans.UnitTests.Extensions;
using CG.Web.MegaApiClient;
using FakeItEasy;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace BotDeScans.UnitTests.Specs.Features.Mega.InternalServices;

public class MegaFilesServiceTests : UnitTest
{
    private readonly MegaFilesService service;

    public MegaFilesServiceTests()
    {
        fixture.FreezeFake<MegaClient>();
        fixture.FreezeFake<MegaResourcesService>();
        fixture.FreezeFake<StreamWrapper>();

        A.CallTo(() => fixture
            .FreezeFake<MegaClient>().Client)
            .Returns(fixture.FreezeFake<IMegaApiClient>());

        service = fixture.Create<MegaFilesService>();
    }

    public class GetAsync : MegaFilesServiceTests
    {
        public GetAsync()
        {
            A.CallTo(() => fixture
                .FreezeFake<MegaResourcesService>()
                .GetResourcesAsync(
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    NodeType.File))
                .Returns([fixture.FreezeFake<INode>()]);
        }

        [Fact]
        public async Task GivenExistingSingleNodeShouldReturnIt()
        {
            var result = await service.GetAsync(fixture.Create<string>(), A.Fake<INode>());

            result.Should().BeSuccess().And.HaveValue(fixture.FreezeFake<INode>());
        }

        [Fact]
        public async Task GivenNoneExistingNodeShouldReturnNull()
        {
            A.CallTo(() => fixture
                .FreezeFake<MegaResourcesService>()
                .GetResourcesAsync(
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    NodeType.File))
                .Returns([]);

            var result = await service.GetAsync(fixture.Create<string>(), A.Fake<INode>());

            result.Should().BeSuccess().And.HaveValue(null);
        }

        [Fact]
        public async Task GivenMultipleResultsShouldReturnFailResult()
        {
            var parentNode = A.Fake<INode>();
            var parentNodeId = fixture.Create<string>();
            var parentNodeName = fixture.Create<string>();
            var fileName = fixture.Create<string>();

            A.CallTo(() => parentNode.Id).Returns(parentNodeId);
            A.CallTo(() => parentNode.Name).Returns(parentNodeName);
            A.CallTo(() => fixture
                .FreezeFake<MegaResourcesService>()
                .GetResourcesAsync(
                    fileName,
                    parentNodeId,
                    NodeType.File))
                .Returns([fixture.FreezeFake<INode>(), fixture.FreezeFake<INode>()]);

            var result = await service.GetAsync(fileName, parentNode);

            result.Should().BeFailure().And.HaveError(
                $"Mais de um resultado foi encontrado para a busca de arquivos no Mega. " +
                $"fileName: {fileName}, parent: {parentNodeName}");
        }
    }

    public class UploadAsync : MegaFilesServiceTests
    {
        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnExpectedResult()
        {
            var filePath = Path.Combine("dir", "file.zip");
            var parentNode = A.Fake<INode>();

            A.CallTo(() => fixture
                .FreezeFake<StreamWrapper>()
                .CreateFileStream(filePath, FileMode.Open))
                .Returns(fixture.FreezeFake<Stream>());

            A.CallTo(() => fixture
                .FreezeFake<IMegaApiClient>()
                .UploadAsync(
                    fixture.FreezeFake<Stream>(),
                    "file.zip",
                    parentNode,
                    null,
                    null,
                    CancellationToken.None))
                .Returns(fixture.FreezeFake<INode>());

            A.CallTo(() => fixture
                .FreezeFake<IMegaApiClient>()
                .GetDownloadLinkAsync(fixture.FreezeFake<INode>()))
                .Returns(fixture.FreezeFake<Uri>());

            var result = await service.UploadAsync(
                filePath,
                parentNode,
                CancellationToken.None);

            result.Should().Be(fixture.FreezeFake<Uri>());
        }
    }

    public class DeleteAsync : MegaFilesServiceTests
    {
        [Fact]
        public async Task GivenRequestShouldCallDeleteResourceMethod()
        {
            await service.DeleteAsync(fixture.FreezeFake<INode>());

            A.CallTo(() => fixture
                .FreezeFake<MegaResourcesService>()
                .DeleteAsync(fixture.FreezeFake<INode>()))
                .MustHaveHappenedOnceExactly();
        }
    }
}
