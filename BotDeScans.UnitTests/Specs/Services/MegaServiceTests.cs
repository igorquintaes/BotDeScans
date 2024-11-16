using BotDeScans.App.Services;
using BotDeScans.App.Services.ExternalClients;
using BotDeScans.App.Services.Wrappers;
using CG.Web.MegaApiClient;
using FakeItEasy;
using FluentAssertions;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
namespace BotDeScans.UnitTests.Specs.Services;

public class MegaServiceTests : UnitTest<MegaService>
{
    private readonly IMegaApiClient megaApiClient;
    private readonly StreamWrapper streamWrapper;
    private readonly IConfiguration configuration;

    public MegaServiceTests()
    {
        streamWrapper = A.Fake<StreamWrapper>();
        configuration = A.Fake<IConfiguration>();
        megaApiClient = A.Fake<IMegaApiClient>();

        var megaClient = A.Fake<MegaClient>();
        A.CallTo(() => megaClient.Client).Returns(megaApiClient);

        instance = new(megaClient, streamWrapper, configuration);

    }

    public class GetOrCreateFolderAsync : MegaServiceTests
    {
        private readonly INode[] nodes;

        public GetOrCreateFolderAsync()
        {
            nodes = new INode[]
            {
                A.Fake<INode>(),
                A.Fake<INode>(),
                A.Fake<INode>(),
            };

            A.CallTo(() => nodes[0].Type).Returns(NodeType.Root);
            A.CallTo(() => nodes[0].Id).Returns(dataGenerator.Random.Word());
            A.CallTo(() => nodes[1].Name).Returns(dataGenerator.Random.Word());
            A.CallTo(() => nodes[1].Id).Returns(dataGenerator.Random.Word());
            A.CallTo(() => nodes[1].ParentId).Returns(nodes[0].Id);
            A.CallTo(() => nodes[1].Type).Returns(NodeType.Directory);
            A.CallTo(() => nodes[2].Name).Returns(dataGenerator.Random.Word());
            A.CallTo(() => nodes[2].Id).Returns(dataGenerator.Random.Word());
            A.CallTo(() => nodes[2].Type).Returns(NodeType.Directory);

            A.CallTo(() => megaApiClient
                .GetNodesAsync())
                .Returns(nodes);
        }

        [Fact]
        public async Task ShouldGetFolderWhenItExists()
        {
            var result = await instance.GetOrCreateFolderAsync(nodes[1].Name);
            result.Should().Be(nodes[1]);
            A.CallTo(() => megaApiClient
                .CreateFolderAsync(A<string>.Ignored, A<INode>.Ignored))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task ShouldGetFolderWhenItExists_WithParentFolder()
        {
            A.CallTo(() => nodes[1].ParentId).Returns(nodes[2].Id);
            var result = await instance.GetOrCreateFolderAsync(nodes[1].Name, nodes[2]);
            result.Should().Be(nodes[1]);
            A.CallTo(() => megaApiClient
                .CreateFolderAsync(A<string>.Ignored, A<INode>.Ignored))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task ShouldCreateFolderWhenItDoesNotExists()
        {
            var folderName = dataGenerator.Random.Word();
            var newNode = A.Fake<INode>();
            A.CallTo(() => megaApiClient
                .CreateFolderAsync(folderName, nodes[0]))
                .Returns(newNode);

            var result = await instance.GetOrCreateFolderAsync(folderName);
            result.Should().Be(newNode);
        }

        [Fact]
        public async Task ShouldCreateFolderWhenItDoesNotExists_WithParentFolder()
        {
            var folderName = dataGenerator.Random.Word();
            var newNode = A.Fake<INode>();
            var parentNode = A.Fake<INode>();
            A.CallTo(() => megaApiClient
                .CreateFolderAsync(folderName, parentNode))
                .Returns(newNode);

            var result = await instance.GetOrCreateFolderAsync(folderName, parentNode);
            result.Should().Be(newNode);
        }

        [Theory]
        [InlineData(NodeType.File)]
        [InlineData(NodeType.Inbox)]
        [InlineData(NodeType.Root)]
        [InlineData(NodeType.Trash)]
        public async Task ShouldFilterFoldersBasedOnDirectoryType(NodeType nodeType)
        {
            A.CallTo(() => nodes[1].Type).Returns(nodeType);

            var newNode = A.Fake<INode>();
            A.CallTo(() => megaApiClient
                .CreateFolderAsync(nodes[1].Name, nodes[0]))
                .Returns(newNode);

            var result = await instance.GetOrCreateFolderAsync(nodes[1].Name);
            result.Should().Be(newNode);
        }
    }

    public class CreateFileAsync : MegaServiceTests, IDisposable
    {
        private readonly string filePath;
        private readonly Stream stream;
        private readonly INode fileNode;
        private readonly Uri uri;

        public CreateFileAsync()
        {
            filePath = Path.Combine("C:", "some-path", "some-file.jpg");
            stream = A.Fake<Stream>();
            fileNode = A.Fake<INode>();
            uri = new Uri("http://www.google.com");

            A.CallTo(() => streamWrapper
                .CreateFileStream(filePath, FileMode.Open))
                .Returns(stream);
        }

        [Fact]
        public async Task ShouldCreateFileSuccessfuly()
        {
            A.CallTo(() => megaApiClient
                .UploadAsync(stream, "some-file.jpg", null, null, null, cancellationToken))
                .Returns(fileNode);

            A.CallTo(() => megaApiClient
                .GetDownloadLinkAsync(fileNode))
                .Returns(uri);

            var result = await instance.CreateFileAsync(filePath, null, cancellationToken);
            result.Should().BeSuccess().And.HaveValue(uri);
        }

        [Fact]
        public async Task ShouldCreateFileSuccessfuly_WithParentFolder()
        {
            var parentNode = A.Fake<INode>();

            A.CallTo(() => megaApiClient
                .UploadAsync(stream, "some-file.jpg", null, null, null, cancellationToken))
                .Throws<Exception>();

            A.CallTo(() => megaApiClient
                .UploadAsync(stream, "some-file.jpg", parentNode, null, null, cancellationToken))
                .Returns(fileNode);

            A.CallTo(() => megaApiClient
                .GetDownloadLinkAsync(fileNode))
                .Returns(uri);

            var result = await instance.CreateFileAsync(filePath, parentNode, cancellationToken);
            result.Should().BeSuccess().And.HaveValue(uri);
        }

        public override void Dispose()
        {
            stream?.Dispose();
            base.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
