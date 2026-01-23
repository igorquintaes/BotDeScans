using BotDeScans.App.Services;
using BotDeScans.App.Services.Wrappers;
using Box.V2;
using Box.V2.Managers;
using Box.V2.Models;

namespace BotDeScans.UnitTests.Specs.Services;

public class BoxServiceTests : UnitTest
{
    private readonly BoxService service;

    public BoxServiceTests()
    {
        fixture.FreezeFake<IBoxClient>();
        fixture.FreezeFake<StreamWrapper>();

        service = fixture.Create<BoxService>();
    }

    public class GetOrCreateFolderAsync : BoxServiceTests
    {
        private const string FOLDER_TYPE = "folder";
        private const int MAX_ITEMS_QUERY = 1000;
        private readonly string folderName;
        private readonly string parentFolderId;

        public GetOrCreateFolderAsync()
        {
            folderName = fixture.Create<string>();
            parentFolderId = fixture.Create<string>();

            fixture.FreezeFake<IBoxFoldersManager>();

            A.CallTo(() => fixture
                .FreezeFake<IBoxClient>()
                .FoldersManager)
                .Returns(fixture.FreezeFake<IBoxFoldersManager>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionForExistingFolderShouldReturnExistingFolder()
        {
            var existingFolder = fixture.FreezeFake<BoxFolder>();
            var boxCollection = new BoxCollection<BoxItem>
            {
                Entries =
                [
                    fixture.Create<BoxFolder>(),
                    existingFolder
                ]
            };

            A.CallTo(() => existingFolder.Name).Returns(folderName);
            A.CallTo(() => existingFolder.Type).Returns(FOLDER_TYPE);
            A.CallTo(() => fixture
                .FreezeFake<IBoxFoldersManager>()
                .GetFolderItemsAsync(
                    parentFolderId,
                    MAX_ITEMS_QUERY,
                    default, default, default, default,
                    default, default, default))
                .Returns(boxCollection);

            var result = await service.GetOrCreateFolderAsync(folderName, parentFolderId);

            result.Should().Be(existingFolder);

            A.CallTo(() => fixture
                .FreezeFake<IBoxFoldersManager>()
                .CreateAsync(A<BoxFolderRequest>.Ignored, null))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionForNewFolderShouldReturnCreatedFolder()
        {
            var newFolder = fixture.Create<BoxFolder>();
            var boxCollection = new BoxCollection<BoxItem>
            {
                Entries =
                [
                    fixture.Create<BoxFolder>(),
                    fixture.Create<BoxFolder>()
                ]
            };

            A.CallTo(() => fixture
                .FreezeFake<IBoxFoldersManager>()
                .GetFolderItemsAsync(
                    parentFolderId,
                    MAX_ITEMS_QUERY,
                    default, default, default,
                    default, default, default, default))
                .Returns(boxCollection);

            A.CallTo(() => fixture
                .FreezeFake<IBoxFoldersManager>()
                .CreateAsync(
                    A<BoxFolderRequest>.That.Matches(x =>
                        x.Name == folderName &&
                        x.Parent.Id == parentFolderId),
                    null))
                .Returns(newFolder);

            var result = await service.GetOrCreateFolderAsync(folderName, parentFolderId);

            result.Should().Be(newFolder);
        }

        [Fact]
        public async Task GivenDefaultParentFolderIdShouldUseRootId()
        {
            A.CallTo(() => fixture
                .FreezeFake<IBoxFoldersManager>()
                .CreateAsync(
                    A<BoxFolderRequest>.That.Matches(x => x.Parent.Id == BoxService.ROOT_ID),
                    null))
                .Returns(fixture.Create<BoxFolder>());

            await service.GetOrCreateFolderAsync(folderName);

            A.CallTo(() => fixture
                .FreezeFake<IBoxFoldersManager>()
                .GetFolderItemsAsync(
                    BoxService.ROOT_ID,
                    MAX_ITEMS_QUERY,
                    default, default, default,
                    default, default, default, default))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenMultipleFoldersWithSameNameShouldReturnFirstMatch()
        {
            var firstMatch = fixture.FreezeFake<BoxFolder>();
            var secondMatch = fixture.Create<BoxFolder>();
            var boxCollection = new BoxCollection<BoxItem>
            {
                Entries =
                [
                    firstMatch,
                    secondMatch
                ]
            };

            A.CallTo(() => firstMatch.Name).Returns(folderName);
            A.CallTo(() => firstMatch.Type).Returns(FOLDER_TYPE);
            A.CallTo(() => secondMatch.Name).Returns(folderName);
            A.CallTo(() => secondMatch.Type).Returns(FOLDER_TYPE);

            A.CallTo(() => fixture
                .FreezeFake<IBoxFoldersManager>()
                .GetFolderItemsAsync(
                    parentFolderId,
                    MAX_ITEMS_QUERY,
                    default, default, default,
                    default, default, default, default))
                .Returns(boxCollection);

            var result = await service.GetOrCreateFolderAsync(folderName, parentFolderId);

            result.Should().Be(firstMatch);
        }

        [Fact]
        public async Task GivenFolderWithSameNameButDifferentTypeShouldCreateNewFolder()
        {
            var fileWithSameName = A.Fake<BoxFile>();
            A.CallTo(() => fileWithSameName.Name).Returns(folderName);
            A.CallTo(() => fileWithSameName.Type).Returns("file");

            var newFolder = fixture.Create<BoxFolder>();
            var boxCollection = new BoxCollection<BoxItem>
            {
                Entries = [fileWithSameName]
            };

            A.CallTo(() => fixture
                .FreezeFake<IBoxFoldersManager>()
                .GetFolderItemsAsync(
                    parentFolderId,
                    MAX_ITEMS_QUERY,
                    default, default, default,
                    default, default, default, default))
                .Returns(boxCollection);

            A.CallTo(() => fixture
                .FreezeFake<IBoxFoldersManager>()
                .CreateAsync(A<BoxFolderRequest>.Ignored, null))
                .Returns(newFolder);

            var result = await service.GetOrCreateFolderAsync(folderName, parentFolderId);

            result.Should().Be(newFolder);
        }
    }

    public class CreateFileAsync : BoxServiceTests
    {
        private readonly string filePath;
        private readonly string fileName;
        private readonly string parentFolderId;
        private readonly Stream stream;

        public CreateFileAsync()
        {
            fileName = "test-file.zip";
            filePath = Path.Combine("directory", fileName);
            parentFolderId = fixture.Create<string>();
            stream = fixture.FreezeFake<Stream>();

            fixture.FreezeFake<IBoxFilesManager>();

            A.CallTo(() => fixture
                .FreezeFake<IBoxClient>().FilesManager)
                .Returns(fixture.FreezeFake<IBoxFilesManager>());

            A.CallTo(() => fixture
                .FreezeFake<StreamWrapper>()
                .CreateFileStream(filePath, FileMode.Open))
                .Returns(stream);

            A.CallTo(() => fixture
                .FreezeFake<IBoxFilesManager>()
                .UploadAsync(
                    A<BoxFileRequest>.Ignored,
                    stream,
                    default, default, default,
                    true,
                    default))
                .Returns(fixture.FreezeFake<BoxFile>());

            A.CallTo(() => fixture
                .FreezeFake<BoxFile>().Id)
                .Returns(fixture.Create<string>());

            A.CallTo(() => fixture
                .FreezeFake<IBoxFilesManager>()
                .CreateSharedLinkAsync(
                    A<string>.Ignored,
                    A<BoxSharedLinkRequest>.Ignored,
                    null))
                .Returns(fixture.Create<BoxFile>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnBoxFileWithSharedLink()
        {
            var sharedFile = fixture.Create<BoxFile>();

            A.CallTo(() => fixture
                .FreezeFake<IBoxFilesManager>()
                .CreateSharedLinkAsync(
                    A<string>.Ignored,
                    A<BoxSharedLinkRequest>.Ignored,
                    null))
                .Returns(sharedFile);

            var result = await service.CreateFileAsync(filePath, parentFolderId);

            result.Should().Be(sharedFile);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldUploadFileWithCorrectRequest()
        {
            await service.CreateFileAsync(filePath, parentFolderId);

            A.CallTo(() => fixture
                .FreezeFake<IBoxFilesManager>()
                .UploadAsync(
                    A<BoxFileRequest>.That.Matches(x =>
                        x.Name == fileName &&
                        x.Parent.Id == parentFolderId),
                    stream,
                    default, default, default,
                    true,
                    default))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCreateSharedLinkWithCorrectPermissions()
        {
            await service.CreateFileAsync(filePath, parentFolderId);

            A.CallTo(() => fixture
                .FreezeFake<IBoxFilesManager>()
                .CreateSharedLinkAsync(
                    fixture.FreezeFake<BoxFile>().Id,
                    A<BoxSharedLinkRequest>.That.Matches(x =>
                        x.Access == BoxSharedLinkAccessType.open &&
                        x.Permissions.Download == true &&
                        x.UnsharedAt == null),
                    null))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenDefaultParentFolderIdShouldUseRootId()
        {
            await service.CreateFileAsync(filePath);

            A.CallTo(() => fixture
                .FreezeFake<IBoxFilesManager>()
                .UploadAsync(
                    A<BoxFileRequest>.That.Matches(x => x.Parent.Id == BoxService.ROOT_ID),
                    stream,
                    default, default, default,
                    true,
                    default))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldOpenFileStreamCorrectly()
        {
            await service.CreateFileAsync(filePath, parentFolderId);

            A.CallTo(() => fixture
                .FreezeFake<StreamWrapper>()
                .CreateFileStream(filePath, FileMode.Open))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenFilePathShouldExtractCorrectFileName()
        {
            var complexPath = Path.Combine("C:", "Users", "Test", "Documents", "my-file.pdf");
            var expectedFileName = "my-file.pdf";

            A.CallTo(() => fixture
                .FreezeFake<StreamWrapper>()
                .CreateFileStream(complexPath, FileMode.Open))
                .Returns(stream);

            await service.CreateFileAsync(complexPath, parentFolderId);

            A.CallTo(() => fixture
                .FreezeFake<IBoxFilesManager>()
                .UploadAsync(
                    A<BoxFileRequest>.That.Matches(x => x.Name == expectedFileName),
                    stream,
                    default, default, default,
                    true,
                    default))
                .MustHaveHappenedOnceExactly();
        }
    }
}