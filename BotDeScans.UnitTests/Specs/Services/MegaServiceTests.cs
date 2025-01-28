using AutoFixture;
using Bogus.DataSets;
using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Services;
using BotDeScans.App.Services.ExternalClients;
using BotDeScans.App.Services.Wrappers;
using BotDeScans.UnitTests.Specs.Extensions;
using CG.Web.MegaApiClient;
using FakeItEasy;
using FluentAssertions;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using Microsoft.Extensions.Configuration;
using Remora.Commands.Trees.Nodes;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace BotDeScans.UnitTests.Specs.Services;

public class MegaServiceTests : UnitTest
{
    private readonly MegaService service;

    public MegaServiceTests()
    {
        fixture.Fake<MegaClient>();
        fixture.Fake<StreamWrapper>();
        fixture.Fake<IConfiguration>();
        fixture.Fake<IMegaApiClient>();

        A.CallTo(() => fixture
            .Fake<MegaClient>().Client)
            .Returns(fixture.Fake<IMegaApiClient>());

        service = fixture.Create<MegaService>();
    }

    public class GetOrCreateFolderAsync : MegaServiceTests
    {
        private readonly INode rootNode;

        public GetOrCreateFolderAsync()
        {
            rootNode = A.Fake<INode>();
            A.CallTo(() => rootNode.Id).Returns(dataGenerator.Random.Word());
            A.CallTo(() => rootNode.Name).Returns(null);
            A.CallTo(() => rootNode.Type).Returns(NodeType.Root);
        }

        [Theory]
        [InlineData("foldername")]
        [InlineData("FolDerNAMe")]
        public async Task ShouldGetFolderWhenItExists(string folderName)
        {
            const string nodeFolderName = "foldername";
            var folderNode = A.Fake<INode>();
            A.CallTo(() => folderNode.Name).Returns(nodeFolderName);
            A.CallTo(() => folderNode.ParentId).Returns(rootNode.Id);
            A.CallTo(() => folderNode.Type).Returns(NodeType.Directory);

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .GetNodesAsync())
                .Returns([rootNode, folderNode]);

            var result = await service.GetOrCreateFolderAsync(folderName);
            result.Should().Be(folderNode);

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .CreateFolderAsync(A<string>.Ignored, A<INode>.Ignored))
                .MustNotHaveHappened();
        }

        [Theory]
        [InlineData("foldername")]
        [InlineData("FolDerNAMe")]
        public async Task ShouldGetFolderWhenItExists_WithParentFolder(string folderName)
        {
            const string nodeFolderName = "foldername";
            var parentNode = A.Fake<INode>();
            A.CallTo(() => parentNode.Id).Returns(dataGenerator.Random.Word());
            A.CallTo(() => parentNode.Type).Returns(NodeType.Directory);

            var folderNode = A.Fake<INode>();
            A.CallTo(() => folderNode.Name).Returns(nodeFolderName);
            A.CallTo(() => folderNode.ParentId).Returns(parentNode.Id);
            A.CallTo(() => folderNode.Type).Returns(NodeType.Directory);

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .GetNodesAsync())
                .Returns([parentNode, folderNode]);

            var result = await service.GetOrCreateFolderAsync(
                folderName: folderName, 
                parentFolder: parentNode);

            result.Should().Be(folderNode);

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .CreateFolderAsync(A<string>.Ignored, A<INode>.Ignored))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task ShouldCreateFolderWhenItDoesNotExists()
        {
            const string newFolderName = "foldername";
            var newNode = A.Fake<INode>();

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .GetNodesAsync())
                .Returns([rootNode]);

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .CreateFolderAsync(newFolderName, rootNode))
                .Returns(newNode);

            var result = await service.GetOrCreateFolderAsync(newFolderName);

            result.Should().Be(newNode);
        }

        [Fact]
        public async Task ShouldCreateFolderWhenItDoesNotExists_WithParentFolder()
        {
            const string nodeFolderName = "foldername";
            var newNode = A.Fake<INode>();
            var parentNode = A.Fake<INode>();
            A.CallTo(() => parentNode.Id).Returns(dataGenerator.Random.Word());
            A.CallTo(() => parentNode.Type).Returns(NodeType.Directory);

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .GetNodesAsync())
                .Returns([rootNode, parentNode]);

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .CreateFolderAsync(nodeFolderName, parentNode))
                .Returns(newNode);

            var result = await service.GetOrCreateFolderAsync(nodeFolderName, parentNode);

            result.Should().Be(newNode);
        }

        [Theory]
        [InlineData(NodeType.File)]
        [InlineData(NodeType.Inbox)]
        [InlineData(NodeType.Root)]
        [InlineData(NodeType.Trash)]
        public async Task ShouldFilterFoldersBasedOnDirectoryType(NodeType nodeType)
        {
            const string name = "name";
            var newNode = A.Fake<INode>();
            var notFolderNode = A.Fake<INode>();
            A.CallTo(() => notFolderNode.Name).Returns(name);
            A.CallTo(() => notFolderNode.Type).Returns(nodeType);

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .GetNodesAsync())
                .Returns([rootNode, notFolderNode]);

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .CreateFolderAsync(name, rootNode))
                .Returns(newNode);

            var result = await service.GetOrCreateFolderAsync(name);
            result.Should().Be(newNode);
        }
    }

    public class CreateFileAsync : MegaServiceTests, IDisposable
    {
        private readonly string fileName = "some-file.jpg";
        private readonly string filePath = Path.Combine("C:", "some-path", "some-file.jpg");
        private readonly INode rootNode;

        public CreateFileAsync()
        {
            rootNode = A.Fake<INode>();
            A.CallTo(() => rootNode.Id).Returns("rootNode.Id");
            A.CallTo(() => rootNode.Type).Returns(NodeType.Root); 
            
            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .GetNodesAsync(rootNode))
                .Returns([]);

            A.CallTo(() => fixture
                .Fake<IConfiguration>()
                .GetSection(MegaService.REWRITE_KEY))
                .Returns(fixture.Fake<IConfigurationSection>());

            A.CallTo(() => fixture
                .Fake<IConfigurationSection>().Value)
                .Returns("true");

            A.CallTo(() => fixture
                .Fake<StreamWrapper>()
                .CreateFileStream(filePath, FileMode.Open))
                .Returns(fixture.Fake<Stream>());

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .UploadAsync(fixture.Fake<Stream>(), fileName, rootNode, null, null, cancellationToken))
                .Returns(fixture.Fake<INode>());

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .GetDownloadLinkAsync(fixture.Fake<INode>()))
                .Returns(fixture.Fake<Uri>());
        }

        [Fact]
        public async Task ShouldCreateFileSuccessfulyWhenFileDoesNotExists()
        {
            var result = await service.CreateFileAsync(filePath, rootNode, cancellationToken);
            result.Should().BeSuccess().And.HaveValue(fixture.Fake<Uri>());

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .DeleteAsync(A<INode>.Ignored, A<bool>.Ignored))
                .MustNotHaveHappened();

        }

        [Theory]
        [InlineData("some-file.jpg")]
        [InlineData("SoMe-FIle.jPG")]
        public async Task ShouldDeleteAndCreateFileWhenItAlreadyExistsAndAllowsRewrite(string inputFileName)
        {
            var filePath = Path.Combine("C:", "some-path", inputFileName);
            var fileNode = A.Fake<INode>();
            const string fileNodeName = "some-file.jpg";
            A.CallTo(() => fileNode.ParentId).Returns(rootNode.Id);
            A.CallTo(() => fileNode.Type).Returns(NodeType.File);
            A.CallTo(() => fileNode.Name).Returns(fileNodeName);

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .GetNodesAsync(rootNode))
                .Returns([fileNode]);

            A.CallTo(() => fixture
                .Fake<StreamWrapper>()
                .CreateFileStream(filePath, FileMode.Open))
                .Returns(fixture.Fake<Stream>());

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .UploadAsync(fixture.Fake<Stream>(), inputFileName, rootNode, null, null, cancellationToken))
                .Returns(fixture.Fake<INode>());

            var result = await service.CreateFileAsync(filePath, rootNode, cancellationToken);
            result.Should().BeSuccess().And.HaveValue(fixture.Fake<Uri>());

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .DeleteAsync(fileNode, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("false")]

        public async Task ShouldReturnFailResultWhenItAlreadyExistsAndDoesNotAllowsRewrite(string? rewriteKey)
        {
            var fileNode = A.Fake<INode>();
            A.CallTo(() => fileNode.ParentId).Returns(rootNode.Id);
            A.CallTo(() => fileNode.Type).Returns(NodeType.File);
            A.CallTo(() => fileNode.Name).Returns(fileName);

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .GetNodesAsync(rootNode))
                .Returns([fileNode]);

            A.CallTo(() => fixture
                .Fake<IConfigurationSection>().Value)
                .Returns(rewriteKey);

            var result = await service.CreateFileAsync(filePath, rootNode, cancellationToken);
            result.Should().BeFailure().And.HaveError($"Já existe um arquivo com o nome especificado. Se desejar sobrescrever o arquivo existente, altere a configuração {MegaService.REWRITE_KEY} para permitir.");

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .DeleteAsync(A<INode>.Ignored, A<bool>.Ignored))
                .MustNotHaveHappened();

            A.CallTo(() => fixture
                .Fake<IMegaApiClient>()
                .UploadAsync(
                    A<Stream>.Ignored, 
                    A<string>.Ignored, 
                    A<INode>.Ignored, 
                    A<IProgress<double>>.Ignored, 
                    A<DateTime?>.Ignored, 
                    A<CancellationToken>.Ignored))
                .MustNotHaveHappened();
        }

        public void Dispose()
        {
            fixture.Fake<Stream>()?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
