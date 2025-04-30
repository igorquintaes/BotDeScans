using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using FluentAssertions.Execution;
using FluentResults;
using FluentValidation;
using Google.Apis.Drive.v3.Data;
using Microsoft.Extensions.Configuration;
using File = Google.Apis.Drive.v3.Data.File;
namespace BotDeScans.UnitTests.Specs.Features.GoogleDrive;

public class GoogleDriveServiceTests : UnitTest
{
    private readonly GoogleDriveService service;

    public GoogleDriveServiceTests()
    {
        fixture.FreezeFake<GoogleDriveFilesService>();
        fixture.FreezeFake<GoogleDriveFoldersService>();
        fixture.FreezeFake<GoogleDriveResourcesService>();
        fixture.FreezeFake<GoogleDrivePermissionsService>();
        fixture.FreezeFake<IValidator<IList<File>>>();
        fixture.FreezeFake<IConfiguration>();

        service = fixture.Create<GoogleDriveService>();
    }

    public class GetOrCreateFolderAsync : GoogleDriveServiceTests
    {
        private readonly string folderName;
        private readonly string parentId;

        public GetOrCreateFolderAsync()
        {
            folderName = fixture.Create<string>();
            parentId = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .GetAsync(folderName, parentId, cancellationToken))
                .Returns(Result.Ok<File?>(default));

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .CreateAsync(folderName, parentId, cancellationToken))
                .Returns(fixture.FreezeFake<File>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionForNewFolderShouldReturnSuccessResultAndReturnCreatedFolder()
        {
            var result = await service.GetOrCreateFolderAsync(folderName, parentId, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.FreezeFake<File>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .CreateAsync(folderName, parentId, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfullExecutionForExistingFolderShouldReturnSuccessResultAndExistingFolder()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .GetAsync(folderName, parentId, cancellationToken))
                .Returns(fixture.FreezeFake<File>());

            var result = await service.GetOrCreateFolderAsync(folderName, parentId, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.FreezeFake<File>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .CreateAsync(folderName, parentId, cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenErrorWhileCheckingIfFolderExistsShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .GetAsync(folderName, parentId, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.GetOrCreateFolderAsync(folderName, parentId, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorWhileCreatingFolderExistsShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .CreateAsync(folderName, parentId, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.GetOrCreateFolderAsync(folderName, parentId, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }

    public class CreateFileAsync : GoogleDriveServiceTests
    {
        private readonly string filePath = System.IO.Path.Combine("directory", "file.zip");
        private readonly string fileName = "file.zip";

        private readonly string parentId;
        private readonly bool publicAccess;

        public CreateFileAsync()
        {
            parentId = fixture.Create<string>();
            publicAccess = fixture.Create<bool>();

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .GetAsync(fileName, parentId, cancellationToken))
                .Returns(null as File);

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .UploadAsync(filePath, parentId, publicAccess, cancellationToken))
                .Returns(fixture.FreezeFake<File>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .UpdateAsync(filePath, fixture.FreezeFake<File>().Id, cancellationToken))
                .Returns(fixture.FreezeFake<File>());

            fixture.FreezeFakeConfiguration(GoogleDriveService.REWRITE_KEY, "true");
        }

        [Fact]
        public async Task GivenExecutionSuccessfulForANewFileShouldReturnSuccessResultAndNewFileValue()
        {
            var result = await service.CreateFileAsync(filePath, parentId, publicAccess, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.FreezeFake<File>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .UploadAsync(filePath, parentId, publicAccess, cancellationToken))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .UpdateAsync(A<string>.Ignored, A<string>.Ignored, cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenExecutionSuccessfulForAnExistingFileWithRewriteShouldReturnSuccessResultAndUpdatedFileValue()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .GetAsync(fileName, parentId, cancellationToken))
                .Returns(fixture.FreezeFake<File>());

            var result = await service.CreateFileAsync(filePath, parentId, publicAccess, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.FreezeFake<File>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .UpdateAsync(filePath, fixture.FreezeFake<File>().Id, cancellationToken))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .UploadAsync(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored, cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenExistingFileAndNotAllowedToRewriteShouldReturnFailResult()
        {
            fixture.FreezeFakeConfiguration(GoogleDriveService.REWRITE_KEY, "false");

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .GetAsync(fileName, parentId, cancellationToken))
                .Returns(fixture.FreezeFake<File>());

            var result = await service.CreateFileAsync(filePath, parentId, publicAccess, cancellationToken);

            result.Should().BeFailure().And.HaveError($"Já existe um arquivo com o nome especificado. Se desejar sobrescrever o arquivo existente, altere a configuração {GoogleDriveService.REWRITE_KEY} para permitir.");
        }

        [Fact]
        public async Task GivenExistingFileAndNotSpecifiedToRewriteShouldNotAllowActionAndReturnFailResult()
        {
            fixture.FreezeFakeConfiguration(GoogleDriveService.REWRITE_KEY, null as string);

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .GetAsync(fileName, parentId, cancellationToken))
                .Returns(fixture.FreezeFake<File>());

            var result = await service.CreateFileAsync(filePath, parentId, publicAccess, cancellationToken);

            result.Should().BeFailure().And.HaveError($"Já existe um arquivo com o nome especificado. Se desejar sobrescrever o arquivo existente, altere a configuração {GoogleDriveService.REWRITE_KEY} para permitir.");
        }

        [Fact]
        public async Task GivenGetFileErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .GetAsync(fileName, parentId, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.CreateFileAsync(filePath, parentId, publicAccess, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenUploadNewFileErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .UploadAsync(filePath, parentId, publicAccess, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.CreateFileAsync(filePath, parentId, publicAccess, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenUpdateErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .GetAsync(fileName, parentId, cancellationToken))
                .Returns(fixture.FreezeFake<File>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .UpdateAsync(filePath, fixture.FreezeFake<File>().Id, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.CreateFileAsync(filePath, parentId, publicAccess, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }

    public class DeleteFileByNameAndParentNameAsync : GoogleDriveServiceTests
    {
        private readonly string fileName;
        private readonly string parentFolderName;
        private readonly File[] resources;

        public DeleteFileByNameAndParentNameAsync()
        {
            GoogleDriveSettingsService.BaseFolderId = fixture.Create<string>();
            fileName = fixture.Create<string>();
            parentFolderName = fixture.Create<string>();
            resources = fixture.FreezeFakes<File>(2);

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .GetAsync(parentFolderName, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(resources[0]);

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .GetAsync(fileName, resources[0].Id, cancellationToken))
                .Returns(resources[1]);

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveResourcesService>()
                .DeleteResource(resources[1].Id, cancellationToken))
                .Returns(resources[1].Id);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await service.DeleteFileByNameAndParentNameAsync(fileName, parentFolderName, cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenNotFoundParentFolderShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "Não foi encontrada uma pasta com o nome especificado.";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .GetAsync(parentFolderName, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(null as File);

            var result = await service.DeleteFileByNameAndParentNameAsync(fileName, parentFolderName, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToGetParentFolderShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFoldersService>()
                .GetAsync(parentFolderName, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.DeleteFileByNameAndParentNameAsync(fileName, parentFolderName, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenNotFoundFileShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "Não foi encontrado um arquivo com o nome especificado.";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .GetAsync(fileName, resources[0].Id, cancellationToken))
                .Returns(null as File);

            var result = await service.DeleteFileByNameAndParentNameAsync(fileName, parentFolderName, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToGetFileShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .GetAsync(fileName, resources[0].Id, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.DeleteFileByNameAndParentNameAsync(fileName, parentFolderName, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToDeleteFileShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveResourcesService>()
                .DeleteResource(resources[1].Id, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.DeleteFileByNameAndParentNameAsync(fileName, parentFolderName, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }

    public class SaveFilesAsync : GoogleDriveServiceTests
    {
        private readonly string folderId;
        private readonly string directory;

        public SaveFilesAsync()
        {
            folderId = fixture.Create<string>();
            directory = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .GetManyAsync(folderId, cancellationToken))
                .Returns(fixture.FreezeFakes<File>(2));

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .DownloadAsync(
                    A<File>.That.Matches(file => fixture.FreezeFake<File[]>().Contains(file)),
                    directory,
                    cancellationToken))
                .Returns(Result.Ok());
        }

        [Fact]
        public async Task GivenSuccessfulExewcutionShouldReturnSuccessResult()
        {
            var result = await service.SaveFilesAsync(folderId, directory, cancellationToken);
            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenErrorWhenObtainingFilesShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .GetManyAsync(folderId, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.SaveFilesAsync(folderId, directory, cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public async Task GivenOneOrMoreErrorsWhenDownloadingFilesShouldReturnFailResult(int errorsCount)
        {
            const string FIRST_ERROR_MESSAGE = "some error";
            const string SECOND_ERROR_MESSAGE = "other error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveFilesService>()
                .DownloadAsync(
                    A<File>.That.Matches(file => fixture.FreezeFake<File[]>().Contains(file)),
                    directory,
                    cancellationToken))
                .ReturnsNextFromSequence(
                    Result.Fail(FIRST_ERROR_MESSAGE),
                    Result.FailIf(errorsCount > 1, SECOND_ERROR_MESSAGE));

            var result = await service.SaveFilesAsync(folderId, directory, cancellationToken);

            using var _ = new AssertionScope();
            result.Should().BeFailure();
            result.Should().HaveError(FIRST_ERROR_MESSAGE);
            result.Errors.Should().HaveCount(errorsCount);
            if (result.Errors.Count > 1)
                result.Should().HaveError(SECOND_ERROR_MESSAGE);
        }
    }

    public class GrantReaderAccessToBotFilesAsync : GoogleDriveServiceTests
    {
        private readonly string email;

        public GrantReaderAccessToBotFilesAsync()
        {
            GoogleDriveSettingsService.BaseFolderId = fixture.Create<string>();
            email = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<GoogleDrivePermissionsService>()
                .GetUserPermissionsAsync(email, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(Result.Ok<IEnumerable<Permission>>([]));

            A.CallTo(() => fixture
                .FreezeFake<GoogleDrivePermissionsService>()
                .CreateUserReaderPermissionAsync(email, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(fixture.FreezeFake<Permission>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await service.GrantReaderAccessToBotFilesAsync(email, cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionButWithAnAlreadyExistingPermissionShouldReturnSuccessResultWithoutCreatingANewOne()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDrivePermissionsService>()
                .GetUserPermissionsAsync(email, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(Result.Ok<IEnumerable<Permission>>([new Permission()]));

            var result = await service.GrantReaderAccessToBotFilesAsync(email, cancellationToken);

            result.Should().BeSuccess();

            A.CallTo(() => fixture
                .FreezeFake<GoogleDrivePermissionsService>()
                .CreateUserReaderPermissionAsync(A<string>.Ignored, A<string>.Ignored, cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenErrorToObtainPermissionsShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDrivePermissionsService>()
                .GetUserPermissionsAsync(email, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.GrantReaderAccessToBotFilesAsync(email, cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToCreatePermissionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDrivePermissionsService>()
                .CreateUserReaderPermissionAsync(email, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.GrantReaderAccessToBotFilesAsync(email, cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }

    public class RevokeReaderAccessToBotFilesAsync : GoogleDriveServiceTests
    {
        private readonly string email;

        public RevokeReaderAccessToBotFilesAsync()
        {
            GoogleDriveSettingsService.BaseFolderId = fixture.Create<string>();
            email = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<GoogleDrivePermissionsService>()
                .GetUserPermissionsAsync(email, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(Result.Ok(fixture.FreezeFake<IEnumerable<Permission>>()));

            A.CallTo(() => fixture
                .FreezeFake<GoogleDrivePermissionsService>()
                .DeleteUserReaderPermissionsAsync(
                    fixture.FreezeFake<IEnumerable<Permission>>(),
                    GoogleDriveSettingsService.BaseFolderId,
                    cancellationToken))
                .Returns(Result.Ok());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await service.RevokeReaderAccessToBotFilesAsync(email, cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenErrorToObtainPermissionsShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDrivePermissionsService>()
                .GetUserPermissionsAsync(email, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.RevokeReaderAccessToBotFilesAsync(email, cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToDeletePermissionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<GoogleDrivePermissionsService>()
                .DeleteUserReaderPermissionsAsync(
                    fixture.FreezeFake<IEnumerable<Permission>>(),
                    GoogleDriveSettingsService.BaseFolderId,
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.RevokeReaderAccessToBotFilesAsync(email, cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}