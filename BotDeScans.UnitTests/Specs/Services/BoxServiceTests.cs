using BotDeScans.App.Services;
using BotDeScans.App.Services.Wrappers;
using Box.Sdk.Gen;
using Box.Sdk.Gen.Managers;
using Box.Sdk.Gen.Schemas;
using FluentResults;
using Task = System.Threading.Tasks.Task;

namespace BotDeScans.UnitTests.Specs.Services;

public class BoxServiceTests : UnitTest
{
    private readonly BoxService service;

    public BoxServiceTests()
    {
        fixture.FreezeFake<StreamWrapper>();
        fixture.FreezeFake<IBoxClient>();

        service = fixture.Create<BoxService>();
    }

    public class GetOrCreateFolderAsync : BoxServiceTests
    {
        private readonly string folderName;
        private readonly Item folderItem;

        public GetOrCreateFolderAsync()
        {
            folderName = fixture.Create<string>();

            fixture.FreezeFake<IFoldersManager>();

            A.CallTo(() => fixture
                .FreezeFake<IBoxClient>().Folders)
                .Returns(fixture.FreezeFake<IFoldersManager>());

            var existingFolder = fixture.CreateCustom<FolderMini>(f => f
                .With(x => x.Name, folderName));

            folderItem = new Item(existingFolder);

            var folderItems = fixture.CreateCustom<Items>(f => f
                .With(x => x.Entries, [folderItem]));

            A.CallTo(() => fixture
                .FreezeFake<IFoldersManager>()
                .GetFolderItemsAsync(
                    BoxService.ROOT_ID,
                    A<GetFolderItemsQueryParams?>.Ignored,
                    A<GetFolderItemsHeaders?>.Ignored,
                    cancellationToken))
                .Returns(folderItems);
        }

        [Fact]
        public async Task GivenExistingFolderShouldReturnIt()
        {
            var otherFolder = fixture.CreateCustom<FolderMini>(f => f
                .With(x => x.Name, fixture.Create<string>()));

            var otherFolderItem = new Item(otherFolder);

            var items = fixture.CreateCustom<Items>(f => f
                .With(x => x.Entries, [otherFolderItem, folderItem]));

            A.CallTo(() => fixture
                .FreezeFake<IFoldersManager>()
                .GetFolderItemsAsync(
                    BoxService.ROOT_ID,
                    A<GetFolderItemsQueryParams?>.Ignored,
                    A<GetFolderItemsHeaders?>.Ignored,
                    cancellationToken))
                .Returns(items);

            var result = await service.GetOrCreateFolderAsync(folderName, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(folderItem.FolderMini!);

            A.CallTo(() => fixture
                .FreezeFake<IFoldersManager>()
                .CreateFolderAsync(
                    A<CreateFolderRequestBody>.Ignored,
                    A<CreateFolderQueryParams?>.Ignored,
                    A<CreateFolderHeaders?>.Ignored,
                    A<CancellationToken?>.Ignored))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenNonExistingFolderShouldCreateAndReturnIt()
        {
            var otherFolder = fixture.CreateCustom<FolderMini>(f => f
                .With(x => x.Name, fixture.Create<string>()));

            var otherFolderItem = new Item(otherFolder);

            var items = fixture.CreateCustom<Items>(f => f
                .With(x => x.Entries, [otherFolderItem, folderItem]));

            var newFolder = fixture.CreateCustom<FolderFull>(f => f
                .With(x => x.Name, folderName));

            A.CallTo(() => fixture
            .FreezeFake<IFoldersManager>()
                .GetFolderItemsAsync(
                    BoxService.ROOT_ID,
                    A<GetFolderItemsQueryParams?>.Ignored,
                    A<GetFolderItemsHeaders?>.Ignored,
                    cancellationToken))
                .Returns(items);

            A.CallTo(() => fixture
                .FreezeFake<IFoldersManager>()
                .CreateFolderAsync(
                    A<CreateFolderRequestBody>.That.Matches(x =>
                        x.Name == folderName &&
                        x.Parent.Id == BoxService.ROOT_ID),
                    A<CreateFolderQueryParams?>.Ignored,
                    A<CreateFolderHeaders?>.Ignored,
                    cancellationToken))
                .Returns(newFolder);

            var result = await service.GetOrCreateFolderAsync(folderName, cancellationToken);

            result.Should().BeSuccess().Which.Value.Should()
                           .BeEquivalentTo(newFolder, options => options.Including(x => x.Name));
        }

        [Fact]
        public async Task GivenMultipleFoldersWithSameNameShouldReturnFirstMatch()
        {
            var firstMatch = fixture.CreateCustom<FolderMini>(f => f
                .With(x => x.Name, folderName)
                .With(x => x.Id, "first-id"));

            var secondMatch = fixture.CreateCustom<FolderMini>(f => f
                .With(x => x.Name, folderName)
                .With(x => x.Id, "second-id"));

            var firstFolderItem = new Item(firstMatch);

            var secondFolderItem = new Item(secondMatch);

            var items = fixture.CreateCustom<Items>(f => f
                .With(x => x.Entries, [firstFolderItem, secondFolderItem]));

            A.CallTo(() => fixture
                .FreezeFake<IFoldersManager>()
                .GetFolderItemsAsync(
                    BoxService.ROOT_ID,
                    A<GetFolderItemsQueryParams?>.Ignored,
                    A<GetFolderItemsHeaders?>.Ignored,
                    cancellationToken))
                .Returns(items);

            var result = await service.GetOrCreateFolderAsync(folderName, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(firstMatch);
            result.Value.Id.Should().Be("first-id");
        }

        [Fact]
        public async Task GivenEmptyEntriesShouldCreateNewFolder()
        {
            var items = fixture.CreateCustom<Items>(f => f
                .With(x => x.Entries, []));

            var newFolder = fixture.CreateCustom<FolderFull>(f => f
                .With(x => x.Name, folderName));

            A.CallTo(() => fixture
                .FreezeFake<IFoldersManager>()
                .GetFolderItemsAsync(
                    BoxService.ROOT_ID,
                    A<GetFolderItemsQueryParams?>.Ignored,
                    A<GetFolderItemsHeaders?>.Ignored,
                    cancellationToken))
                .Returns(items);

            A.CallTo(() => fixture
                .FreezeFake<IFoldersManager>()
                .CreateFolderAsync(
                    A<CreateFolderRequestBody>.Ignored,
                    A<CreateFolderQueryParams?>.Ignored,
                    A<CreateFolderHeaders?>.Ignored,
                    cancellationToken))
                .Returns(newFolder);

            var result = await service.GetOrCreateFolderAsync(folderName, cancellationToken);

            result.Should().BeSuccess().Which.Value.Should()
                           .BeEquivalentTo(newFolder, options => options.Including(x => x.Name));
        }

        [Fact]
        public async Task GivenNullEntriesShouldCreateNewFolder()
        {
            var items = fixture.CreateCustom<Items>(f => f
                .With(x => x.Entries, null));

            var newFolder = fixture.CreateCustom<FolderFull>(f => f
                .With(x => x.Name, folderName));

            A.CallTo(() => fixture
                .FreezeFake<IFoldersManager>()
                .GetFolderItemsAsync(
                    BoxService.ROOT_ID,
                    A<GetFolderItemsQueryParams?>.Ignored,
                    A<GetFolderItemsHeaders?>.Ignored,
                    cancellationToken))
                .Returns(items);

            A.CallTo(() => fixture
                .FreezeFake<IFoldersManager>()
                .CreateFolderAsync(
                    A<CreateFolderRequestBody>.Ignored,
                    A<CreateFolderQueryParams?>.Ignored,
                    A<CreateFolderHeaders?>.Ignored,
                    cancellationToken))
                .Returns(newFolder);

            var result = await service.GetOrCreateFolderAsync(folderName, cancellationToken);

            result.Should().BeSuccess().Which.Value.Should()
                           .BeEquivalentTo(newFolder, options => options.Including(x => x.Name));
        }

        [Fact]
        public async Task GivenExceptionToGetItemsShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";
            var exception = new InvalidOperationException(ERROR_MESSAGE);

            A.CallTo(() => fixture
                .FreezeFake<IFoldersManager>()
                .GetFolderItemsAsync(
                    BoxService.ROOT_ID,
                    A<GetFolderItemsQueryParams?>.Ignored,
                    A<GetFolderItemsHeaders?>.Ignored,
                    cancellationToken))
                .Throws(exception);

            var result = await service.GetOrCreateFolderAsync(folderName, cancellationToken);

            result.Should().BeFailure().And.HaveError(BoxService.GENERIC_ERROR);
            result.Errors.Should().HaveCount(1);
            result.Errors[0].Reasons.Should().HaveCount(1);
            result.Errors[0].Reasons[0].Should().BeOfType<ExceptionalError>();
            result.Errors[0].Reasons[0].As<ExceptionalError>().Exception.Should().Be(exception);
        }

        [Fact]
        public async Task GivenExceptionToCreateItemShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";
            var exception = new InvalidOperationException(ERROR_MESSAGE);

            A.CallTo(() => fixture
                .FreezeFake<IFoldersManager>()
                .GetFolderItemsAsync(
                    BoxService.ROOT_ID,
                    A<GetFolderItemsQueryParams?>.Ignored,
                    A<GetFolderItemsHeaders?>.Ignored,
                    cancellationToken))
                .Returns(new Items());

            A.CallTo(() => fixture
                .FreezeFake<IFoldersManager>()
                .CreateFolderAsync(
                    A<CreateFolderRequestBody>.Ignored,
                    A<CreateFolderQueryParams?>.Ignored,
                    A<CreateFolderHeaders?>.Ignored,
                    cancellationToken))
                .Throws(exception);

            var result = await service.GetOrCreateFolderAsync(folderName, cancellationToken);

            result.Should().BeFailure().And.HaveError(BoxService.GENERIC_ERROR);
            result.Errors.Should().HaveCount(1);
            result.Errors[0].Reasons.Should().HaveCount(1);
            result.Errors[0].Reasons[0].Should().BeOfType<ExceptionalError>();
            result.Errors[0].Reasons[0].As<ExceptionalError>().Exception.Should().Be(exception);
        }
    }

    public class CreateFileAsync : BoxServiceTests
    {
        private readonly string filePath;
        private readonly string fileName;
        private readonly string parentFolderId;
        private readonly string downloadUrl;
        private readonly Stream stream;

        public CreateFileAsync()
        {
            downloadUrl = "https://example.com/download";
            fileName = "test-file.zip";
            filePath = Path.Combine("directory", fileName);
            parentFolderId = fixture.Create<string>();
            stream = fixture.FreezeFake<Stream>();
            fixture.FreezeFake<IUploadsManager>();
            fixture.FreezeFake<IFilesManager>();

            var uploadedFile = fixture.CreateCustom<FileFull>(f => f
                .With(x => x.Id, fixture.Create<string>()));

            var uploadedFiles = fixture.CreateCustom<Files>(f => f
                .With(x => x.Entries, [uploadedFile]));

            var sharedLink = fixture.CreateCustom<FileSharedLinkField>(f => f
                .With(x => x.DownloadUrl, downloadUrl));

            var fileWithSharedLink = fixture.CreateCustom<FileFull>(f => f
                .With(x => x.Id, uploadedFile.Id)
                .With(x => x.SharedLink, sharedLink));

            A.CallTo(() => fixture
                .FreezeFake<IBoxClient>().Uploads)
                .Returns(fixture.FreezeFake<IUploadsManager>());

            A.CallTo(() => fixture
                .FreezeFake<IBoxClient>().Files)
                .Returns(fixture.FreezeFake<IFilesManager>());

            A.CallTo(() => fixture
                .FreezeFake<IUploadsManager>()
                .UploadFileAsync(
                    A<UploadFileRequestBody>.Ignored,
                    A<UploadFileQueryParams?>.Ignored,
                    A<UploadFileHeaders?>.Ignored,
                    cancellationToken))
                .Returns(uploadedFiles);

            A.CallTo(() => fixture
                .FreezeFake<StreamWrapper>()
                .CreateFileStream(filePath, FileMode.Open))
                .Returns(stream);
        }
        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await service.CreateFileAsync(filePath, parentFolderId, cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnFileWithSharedLink()
        {
            var fileWithSharedLink = fixture.Create<FileFull>();

            A.CallTo(() => fixture
                .FreezeFake<IFilesManager>()
                .UpdateFileByIdAsync(
                    A<string>._,
                    A<UpdateFileByIdRequestBody>.Ignored,
                    A<UpdateFileByIdQueryParams?>.Ignored,
                    A<UpdateFileByIdHeaders?>.Ignored,
                    cancellationToken))
                .Returns(fileWithSharedLink);

            var result = await service.CreateFileAsync(filePath, parentFolderId, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fileWithSharedLink);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldUploadFileWithCorrectRequest()
        {
            await service.CreateFileAsync(filePath, parentFolderId, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IUploadsManager>()
                .UploadFileAsync(
                    A<UploadFileRequestBody>.That.Matches(x =>
                        x.Attributes.Name == fileName &&
                        x.Attributes.Parent.Id == parentFolderId &&
                        x.File == stream),
                    A<UploadFileQueryParams?>.Ignored,
                    A<UploadFileHeaders?>.Ignored,
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCreateSharedLinkWithCorrectPermissions()
        {
            await service.CreateFileAsync(filePath, parentFolderId, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IFilesManager>()
                .UpdateFileByIdAsync(
                    A<string>._,
                    A<UpdateFileByIdRequestBody>.That.Matches(x =>
                        x.SharedLink != null &&
                        x.SharedLink.Access!.Value == UpdateFileByIdRequestBodySharedLinkAccessField.Open &&
                        x.SharedLink.Permissions!.CanDownload == true &&
                        x.SharedLink.UnsharedAt == null),
                    A<UpdateFileByIdQueryParams?>.Ignored,
                    A<UpdateFileByIdHeaders?>.Ignored,
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldOpenFileStreamCorrectly()
        {
            await service.CreateFileAsync(filePath, parentFolderId, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<StreamWrapper>()
                .CreateFileStream(filePath, FileMode.Open))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenFilePathShouldExtractCorrectFileName()
        {
            await service.CreateFileAsync(filePath, parentFolderId, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IUploadsManager>()
                .UploadFileAsync(
                    A<UploadFileRequestBody>.That.Matches(x =>
                        x.Attributes.Name == fileName &&
                        x.File == stream),
                    A<UploadFileQueryParams?>.Ignored,
                    A<UploadFileHeaders?>.Ignored,
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenExceptionToUploadFileShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";
            var exception = new InvalidOperationException(ERROR_MESSAGE);

            A.CallTo(() => fixture
                .FreezeFake<IUploadsManager>()
                .UploadFileAsync(
                    A<UploadFileRequestBody>._, 
                    A<UploadFileQueryParams>._, 
                    A<UploadFileHeaders>._, 
                    cancellationToken))
                .Throws(exception);

            var result = await service.CreateFileAsync(filePath, parentFolderId, cancellationToken);

            result.Should().BeFailure().And.HaveError(BoxService.GENERIC_ERROR);
            result.Errors.Should().HaveCount(1);
            result.Errors[0].Reasons.Should().HaveCount(1);
            result.Errors[0].Reasons[0].Should().BeOfType<ExceptionalError>();
            result.Errors[0].Reasons[0].As<ExceptionalError>().Exception.Should().Be(exception);
        }

        [Fact]
        public async Task GivenExceptionToUpdateFileShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";
            var exception = new InvalidOperationException(ERROR_MESSAGE);

            A.CallTo(() => fixture
                .FreezeFake<IFilesManager>()
                .UpdateFileByIdAsync(
                    A<string>._,
                    A<UpdateFileByIdRequestBody>._,
                    A<UpdateFileByIdQueryParams>._,
                    A<UpdateFileByIdHeaders>._,
                    cancellationToken))
                .Throws(exception);

            var result = await service.CreateFileAsync(filePath, parentFolderId, cancellationToken);

            result.Should().BeFailure().And.HaveError(BoxService.GENERIC_ERROR);
            result.Errors.Should().HaveCount(1);
            result.Errors[0].Reasons.Should().HaveCount(1);
            result.Errors[0].Reasons[0].Should().BeOfType<ExceptionalError>();
            result.Errors[0].Reasons[0].As<ExceptionalError>().Exception.Should().Be(exception);
        }
    }
}