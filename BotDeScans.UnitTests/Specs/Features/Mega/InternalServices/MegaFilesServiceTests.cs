using AutoFixture;
using BotDeScans.App.Features.Mega.InternalServices;
using BotDeScans.App.Services.ExternalClients;
using BotDeScans.App.Services.Wrappers;
using BotDeScans.UnitTests.Specs.Extensions;
using CG.Web.MegaApiClient;
using FakeItEasy;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using Remora.Commands.Trees.Nodes;
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
        fixture.Fake<MegaClient>();
        fixture.Fake<MegaResourcesService>();
        fixture.Fake<StreamWrapper>();

        A.CallTo(() => fixture
            .Fake<MegaClient>().Client)
            .Returns(fixture.Fake<IMegaApiClient>());

        service = fixture.Create<MegaFilesService>();
    }

    public class GetAsync : MegaFilesServiceTests
    {
        public GetAsync()
        {
            A.CallTo(() => fixture
                .Fake<MegaResourcesService>()
                .GetResourcesAsync(
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    A<NodeType?>.Ignored))
                .Returns([fixture.Fake<INode>()]);
        }

        [Fact]
        public async Task GivenExistingSingleNodeShouldReturnIt()
        {
            var result = await service.GetAsync(fixture.Create<string>(), A.Fake<INode>());

            result.Should().BeSuccess().And.HaveValue(fixture.Fake<INode>());
        }

        [Fact]
        public async Task GivenNoneExistingNodeShouldReturnNull()
        {
            A.CallTo(() => fixture
                .Fake<MegaResourcesService>()
                .GetResourcesAsync(
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    A<NodeType?>.Ignored))
                .Returns([]);

            var result = await service.GetAsync(fixture.Create<string>(), A.Fake<INode>());

            result.Should().BeSuccess().And.HaveValue(null);
        }

        [Fact]
        public async Task GivenMultipleResultsShouldReturnFailResult()
        {
            var parentNode = A.Fake<INode>();
            var parentNodeId = fixture.Create<string>();
            var fileName = fixture.Create<string>();

            A.CallTo(() => parentNode.Id)
                .Returns(parentNodeId);

            A.CallTo(() => fixture
                .Fake<MegaResourcesService>()
                .GetResourcesAsync(
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    A<NodeType?>.Ignored))
                .Returns([fixture.Fake<INode>(), fixture.Fake<INode>()]);

            var result = await service.GetAsync(fileName, parentNode);

            result.Should().BeFailure().And.HaveError(
                $"Mais de um resultado foi encontrado para a busca de arquivos no Mega. " +
                $"fileName: {fileName}, parent: {parentNode.Name}");
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
                .Fake<StreamWrapper>()
                .CreateFileStream(filePath, FileMode.Open))
                .Returns(fixture.Fake<Stream>());

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .UploadAsync(
                    fixture.Fake<Stream>(),
                    "file.zip",
                    parentNode,
                    null,
                    null,
                    CancellationToken.None))
                .Returns(fixture.Fake<INode>());

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .GetDownloadLinkAsync(fixture.Fake<INode>()))
                .Returns(fixture.Fake<Uri>());

            var result = await service.UploadAsync(
                filePath, 
                parentNode, 
                CancellationToken.None);

            result.Should().Be(fixture.Fake<Uri>());
        }
    }

    public class DeleteAsync : MegaFilesServiceTests
    {
        [Fact]
        public async Task GivenRequestShouldCallDeleteResourceMethod()
        {
            await service.DeleteAsync(fixture.Fake<INode>());

            A.CallTo(() => fixture
                .Fake<MegaResourcesService>()
                .DeleteAsync(fixture.Fake<INode>()))
                .MustHaveHappenedOnceExactly();
        }
    }
}
