using BotDeScans.App.Features.Mega.InternalServices;
using BotDeScans.App.Models.DTOs;
using CG.Web.MegaApiClient;
using FluentAssertions.Execution;
using System.Reflection;
namespace BotDeScans.UnitTests.Specs.Features.Mega.InternalServices;

public class MegaSettingsServiceTests : UnitTest
{
    private readonly MegaSettingsService service;

    public MegaSettingsServiceTests()
    {
        fixture.FreezeFake<IMegaApiClient>();

        service = fixture.Create<MegaSettingsService>();
    }

    public class GetRootNodeAsync : MegaSettingsServiceTests, IDisposable
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
                .Create<MegaSettingsService>();

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
            service.GetType()
                .GetField("Root", BindingFlags.NonPublic | BindingFlags.Static)!
                .SetValue(null, null);

            GC.SuppressFinalize(this);
        }
    }

    public class GetConsumptionDataAsync : MegaSettingsServiceTests
    {
        [Fact]
        public async Task GivenExecutionShouldReturnConsumptionData()
        {
            var accountMetrics = new IStorageMetrics[]
            {
                A.Fake<IStorageMetrics>(),
                A.Fake<IStorageMetrics>(),
                A.Fake<IStorageMetrics>()
            };

            A.CallTo(() => accountMetrics[0].NodeId).Returns("0");
            A.CallTo(() => accountMetrics[1].NodeId).Returns("1");
            A.CallTo(() => accountMetrics[2].NodeId).Returns("2");
            A.CallTo(() => accountMetrics[0].BytesUsed).Returns(0);
            A.CallTo(() => accountMetrics[1].BytesUsed).Returns(1);
            A.CallTo(() => accountMetrics[2].BytesUsed).Returns(2);

            var accountInfo = A.Fake<IAccountInformation>();

            A.CallTo(() => accountInfo.Metrics)
                .Returns(accountMetrics);

            A.CallTo(() => fixture
                .FreezeFake<IMegaApiClient>()
                .GetAccountInformationAsync())
                .Returns(accountInfo);

            var result = await service.GetConsumptionDataAsync(accountMetrics[1].NodeId);

            using var _ = new AssertionScope();
            result.Should().BeSuccess();
            result.ValueOrDefault?.Should().BeEquivalentTo(
                new ConsumptionData(accountMetrics[1].BytesUsed, 21474836479));
        }
    }
}
