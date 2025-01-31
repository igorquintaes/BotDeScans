using AutoFixture;
using BotDeScans.App.Features.Mega.InternalServices;
using BotDeScans.App.Services.ExternalClients;
using BotDeScans.UnitTests.Specs.Extensions;
using CG.Web.MegaApiClient;
using FakeItEasy;
using FluentAssertions;
using FluentAssertions.Execution;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace BotDeScans.UnitTests.Specs.Features.Mega.InternalServices;

public class MegaResourcesServiceTests : UnitTest
{
    private readonly MegaResourcesService service;

    public MegaResourcesServiceTests()
    {
        fixture.Fake<MegaClient>();

        A.CallTo(() => fixture
            .Fake<MegaClient>().Client)
            .Returns(fixture.Fake<IMegaApiClient>());

        service = fixture.Create<MegaResourcesService>();
    }

    public class GetRootNodeAsync : MegaResourcesServiceTests, IDisposable
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
                .Fake<IMegaApiClient>()
                .GetNodesAsync())
                .Returns([
                    rootNode,
                    fileNode,
                    inboxNode,
                    trashNode,
                    directoryNode,
                    undefinedNode]);

            var result = await service.GetRootNodeAsync();

            result.Should().Be(rootNode);
        }

        [Fact]
        public async Task GivenACallToDefineRootNodeShouldReturnItWithoutCallMegaApiAgain()
        {
            var otherServiceInstance = fixture
                .Create<MegaResourcesService>();

            var rootNode = A.Fake<INode>();
            A.CallTo(() => rootNode.Type)
                .Returns(NodeType.Root);

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .GetNodesAsync())
                .Returns([rootNode]);

            var serviceResult = await service.GetRootNodeAsync();
            var otherServiceResult = await otherServiceInstance.GetRootNodeAsync();

            using var _ = new AssertionScope();
            serviceResult.Should().Be(rootNode);
            serviceResult.Should().Be(otherServiceResult);
            service.Should().NotBe(otherServiceInstance);

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .GetNodesAsync())
                .MustHaveHappenedOnceExactly();
        }

        public void Dispose()
        {
            typeof(MegaResourcesService)
                .GetField("Root", BindingFlags.NonPublic | BindingFlags.Static)!
                .SetValue(null, null);

            GC.SuppressFinalize(this);
        }
    }

    public class GetResourcesAsync : MegaResourcesServiceTests
    {
        [Theory]
        [InlineData("requestedName")]
        [InlineData("REQUESTEDNAME")]
        [InlineData("ReQuEStEDnAme")]
        public async Task GivenQueryByNameShouldReturnExpectedResult(string requestedName)
        {
            var expectedNode = A.Fake<INode>();
            A.CallTo(() => expectedNode.Name)
                .Returns(nameof(requestedName));

            var unexpectedNode = A.Fake<INode>();
            A.CallTo(() => unexpectedNode.Name)
                .Returns(nameof(unexpectedNode));

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .GetNodesAsync())
                .Returns([expectedNode, unexpectedNode]);

            var result = await service.GetResourcesAsync(name: requestedName);

            using var _ = new AssertionScope();
            result.Should().HaveCount(1);
            result.FirstOrDefault()?.Should().Be(expectedNode);
        }

        [Fact]
        public async Task GivenQueryByParentIdShouldReturnExpectedResult()
        {
            var expectedNode = A.Fake<INode>();
            A.CallTo(() => expectedNode.ParentId)
                .Returns(nameof(expectedNode));

            var unexpectedNode = A.Fake<INode>();
            A.CallTo(() => unexpectedNode.ParentId)
                .Returns(nameof(unexpectedNode));

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .GetNodesAsync())
                .Returns([expectedNode, unexpectedNode]);

            var result = await service.GetResourcesAsync(parentId: expectedNode.ParentId);

            using var _ = new AssertionScope();
            result.Should().HaveCount(1);
            result.FirstOrDefault()?.Should().Be(expectedNode);
        }

        [Fact]
        public async Task GivenQueryByNodeTypeShouldReturnExpectedResult()
        {
            var expectedNode = A.Fake<INode>();
            A.CallTo(() => expectedNode.Type)
                .Returns(NodeType.File);

            var unexpectedNode = A.Fake<INode>();
            A.CallTo(() => unexpectedNode.Type)
                .Returns(NodeType.Directory);

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .GetNodesAsync())
                .Returns([expectedNode, unexpectedNode]);

            var result = await service.GetResourcesAsync(nodeType: expectedNode.Type);

            using var _ = new AssertionScope();
            result.Should().HaveCount(1);
            result.FirstOrDefault()?.Should().Be(expectedNode);
        }

        [Fact]
        public async Task GivenQueryWithoutFiltersShouldReturnAllResults()
        {
            var expectedReturn = fixture.Fake<INode>(10);

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .GetNodesAsync())
                .Returns(expectedReturn);

            var result = await service.GetResourcesAsync();

            result.Should().BeEquivalentTo(expectedReturn);
        }
    }

    public class DeleteAsync : MegaResourcesServiceTests
    {
        [Fact]
        public async Task ShouldCallDeleteInMegaApi()
        {
            await service.DeleteAsync(fixture.Fake<INode>());

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .DeleteAsync(fixture.Fake<INode>(), false))
                .MustHaveHappenedOnceExactly();
        }
    }
}
