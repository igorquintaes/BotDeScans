using AutoFixture;
using BotDeScans.App.Features.Mega.InternalServices;
using BotDeScans.App.Services.ExternalClients;
using BotDeScans.UnitTests.Extensions;
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
        fixture.FreezeFake<MegaClient>();

        A.CallTo(() => fixture
            .FreezeFake<MegaClient>().Client)
            .Returns(fixture.FreezeFake<IMegaApiClient>());

        service = fixture.Create<MegaResourcesService>();
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
                .FreezeFake<IMegaApiClient>()
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
                .FreezeFake<IMegaApiClient>()
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
                .FreezeFake<IMegaApiClient>()
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
            var expectedReturn = fixture.FreezeFakes<INode>(10);

            A.CallTo(() => fixture
                .FreezeFake<IMegaApiClient>()
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
            await service.DeleteAsync(fixture.FreezeFake<INode>());

            A.CallTo(() => fixture
                .FreezeFake<IMegaApiClient>()
                .DeleteAsync(fixture.FreezeFake<INode>(), false))
                .MustHaveHappenedOnceExactly();
        }
    }
}
