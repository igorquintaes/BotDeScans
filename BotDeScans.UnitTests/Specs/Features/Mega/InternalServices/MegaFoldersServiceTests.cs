using BotDeScans.App.Features.Mega.InternalServices;
using BotDeScans.App.Services.ExternalClients;
using CG.Web.MegaApiClient;
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