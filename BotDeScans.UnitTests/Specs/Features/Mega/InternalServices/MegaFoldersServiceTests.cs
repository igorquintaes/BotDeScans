using AutoFixture;
using BotDeScans.App.Features.Mega.InternalServices;
using BotDeScans.App.Services.ExternalClients;
using BotDeScans.UnitTests.Extensions;
using CG.Web.MegaApiClient;
using FakeItEasy;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentResults.Extensions.FluentAssertions;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
namespace BotDeScans.UnitTests.Specs.Features.Mega.InternalServices;

public class MegaFoldersServiceTests : UnitTest
{
    private readonly MegaFoldersService service;

    public MegaFoldersServiceTests()
    {
        fixture.FreezeFake<MegaClient>();
        fixture.FreezeFake<MegaResourcesService>();

        A.CallTo(() => fixture
            .FreezeFake<MegaClient>().Client)
            .Returns(fixture.FreezeFake<IMegaApiClient>());

        service = fixture.Create<MegaFoldersService>();
    }

    public class GetRootNodeAsync : MegaFoldersServiceTests, IDisposable
    {
        [Fact]
        public async Task GivenUndefinedRootValueCallApiAndReturnRootNode()
        {
            var rootNode = A.Fake<INode>();
            A.CallTo(() => rootNode.Type).Returns(NodeType.Root);

            var fileNode = A.Fake<INode>();
            A.CallTo(() => fileNode.Type).Returns(NodeType.File);

            var inboxNode = A.Fake<INode>();
            A.CallTo(() => inboxNode.Type).Returns(NodeType.Inbox);

            var trashNode = A.Fake<INode>();
            A.CallTo(() => trashNode.Type).Returns(NodeType.Trash);

            var directoryNode = A.Fake<INode>();
            A.CallTo(() => directoryNode.Type).Returns(NodeType.Directory);

            var undefinedNode = A.Fake<INode>();
            A.CallTo(() => undefinedNode.Type).Returns((NodeType)999);


            A.CallTo(() => fixture
                .FreezeFake<IMegaApiClient>()
                .GetNodesAsync())
                .Returns([
                    rootNode,
                    fileNode,
                    inboxNode,
                    trashNode,
                    directoryNode,
                    undefinedNode]);

            var result = await service.GetRootFolderAsync();

            result.Should().Be(rootNode);
        }

        [Fact]
        public async Task GivenACallToDefineRootNodeShouldReturnItWithoutCallMegaApiAgain()
        {
            var otherServiceInstance = fixture
                .Create<MegaFoldersService>();

            var rootNode = A.Fake<INode>();
            A.CallTo(() => rootNode.Type)
                .Returns(NodeType.Root);

            A.CallTo(() => fixture
                .FreezeFake<IMegaApiClient>()
                .GetNodesAsync())
                .Returns([rootNode]);

            var serviceResult = await service.GetRootFolderAsync();
            var otherServiceResult = await otherServiceInstance.GetRootFolderAsync();

            using var _ = new AssertionScope();
            serviceResult.Should().Be(rootNode);
            serviceResult.Should().Be(otherServiceResult);
            service.Should().NotBe(otherServiceInstance);

            A.CallTo(() => fixture
                .FreezeFake<IMegaApiClient>()
                .GetNodesAsync())
                .MustHaveHappenedOnceExactly();
        }

        public void Dispose()
        {
            typeof(MegaFoldersService)
                .GetField("Root", BindingFlags.NonPublic | BindingFlags.Static)!
                .SetValue(null, null);

            GC.SuppressFinalize(this);
        }
    }

    public class GetAsync : MegaFoldersServiceTests
    {
        public GetAsync() =>
            A.CallTo(() => fixture
                .FreezeFake<MegaResourcesService>()
                .GetResourcesAsync(
                    A<string?>.Ignored,
                    A<string?>.Ignored,
                    NodeType.Directory))
                .Returns([fixture.FreezeFake<INode>()]);

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
                    NodeType.Directory))
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
            var folderName = fixture.Create<string>();

            A.CallTo(() => parentNode.Id).Returns(parentNodeId);
            A.CallTo(() => parentNode.Name).Returns(parentNodeName);
            A.CallTo(() => fixture
                .FreezeFake<MegaResourcesService>()
                .GetResourcesAsync(
                    folderName,
                    parentNodeId,
                    NodeType.Directory))
                .Returns([fixture.FreezeFake<INode>(), fixture.FreezeFake<INode>()]);

            var result = await service.GetAsync(folderName, parentNode);

            result.Should().BeFailure().And.HaveError(
                $"Mais de um resultado foi encontrado para a busca de diretórios no Mega. " +
                $"folderName: {folderName}, parent: {parentNodeName}");
        }

        public class CreateAsync : MegaFoldersServiceTests
        {
            [Fact]
            public async Task GivenRequestShouldCallDeleteResourceMethod()
            {
                await service.CreateAsync(fixture.Freeze<string>(), fixture.FreezeFake<INode>());

                A.CallTo(() => fixture
                    .FreezeFake<IMegaApiClient>()
                    .CreateFolderAsync(fixture.Freeze<string>(), fixture.FreezeFake<INode>()))
                    .MustHaveHappenedOnceExactly();
            }
        }

        public class DeleteAsync : MegaFoldersServiceTests
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
}