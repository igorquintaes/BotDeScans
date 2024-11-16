using BotDeScans;
using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services;
using BotDeScans.UnitTests;
using BotDeScans.UnitTests.Specs;
using BotDeScans.UnitTests.Specs.Features.GoogleDrive;
using BotDeScans.UnitTests.Specs.Services;
using FakeItEasy;
using FluentAssertions;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Google.Apis.Drive.v3.Data;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using File = Google.Apis.Drive.v3.Data.File;
namespace BotDeScans.UnitTests.Specs.Features.GoogleDrive;

public class GoogleDriveServiceTests : UnitTest
{
    private readonly GoogleDriveService service;
    private readonly ExtractionService extractionService;
    private readonly GoogleDriveFilesService googleDriveFilesService;
    private readonly GoogleDriveFoldersService googleDriveFoldersService;
    private readonly GoogleDriveResourcesService googleDriveResourcesService;
    private readonly GoogleDrivePermissionsService googleDrivePermissionsService;
    private readonly IConfiguration configuration;
    private readonly IValidator<FileList> validator;

    public GoogleDriveServiceTests()
    {
        extractionService = A.Fake<ExtractionService>();
        googleDriveFilesService = A.Fake<GoogleDriveFilesService>();
        googleDriveFoldersService = A.Fake<GoogleDriveFoldersService>();
        googleDriveResourcesService = A.Fake<GoogleDriveResourcesService>();
        googleDrivePermissionsService = A.Fake<GoogleDrivePermissionsService>();
        configuration = A.Fake<IConfiguration>();
        validator = A.Fake<IValidator<FileList>>();

        GoogleDriveSettingsService.BaseFolderId = dataGenerator.Random.String();

        service = new GoogleDriveService(
            extractionService,
            googleDriveFilesService,
            googleDriveFoldersService,
            googleDriveResourcesService,
            googleDrivePermissionsService,
            validator,
            configuration);
    }

    public class GetOrCreateFolderAsync : GoogleDriveServiceTests
    {
        private readonly string folderName;
        private readonly string parentId;
        private readonly File expectedResult;

        public GetOrCreateFolderAsync()
        {
            folderName = dataGenerator.Random.Word();
            parentId = dataGenerator.Random.Word();
            expectedResult = new File();

            A.CallTo(() => googleDriveFoldersService
                .GetFolderAsync(folderName, parentId, cancellationToken))
                .Returns(null as File);

            A.CallTo(() => googleDriveFoldersService
                .CreateFolderAsync(folderName, parentId, cancellationToken))
                .Returns(expectedResult);
        }

        [Fact]
        public async Task ShouldCreateAndReturnFolderIfItDoesNotExists()
        {
            var result = await service.GetOrCreateFolderAsync(folderName, parentId, cancellationToken);
            result.Should().BeSuccess().And.HaveValue(expectedResult);
        }

        [Fact]
        public async Task ShouldGetFolderIfItExists()
        {
            A.CallTo(() => googleDriveFoldersService
                .GetFolderAsync(folderName, parentId, cancellationToken))
                .Returns(expectedResult);

            var result = await service.GetOrCreateFolderAsync(folderName, parentId, cancellationToken);
            result.Should().BeSuccess().And.HaveValue(expectedResult);
        }

        [Fact]
        public async Task ShouldRepassGetFolderAsyncError()
        {
            var failResult = Result.Fail("some error");
            A.CallTo(() => googleDriveFoldersService
                .GetFolderAsync(folderName, parentId, cancellationToken))
                .Returns(failResult);

            object result = await service.GetOrCreateFolderAsync(folderName, parentId, cancellationToken);
            result.Should().BeEquivalentTo(failResult);
        }

        [Fact]
        public async Task ShouldRepassCreateFolderAsyncError()
        {
            var failResult = Result.Fail("some error");
            A.CallTo(() => googleDriveFoldersService
                .CreateFolderAsync(folderName, parentId, cancellationToken))
                .Returns(failResult);

            object result = await service.GetOrCreateFolderAsync(folderName, parentId, cancellationToken);
            result.Should().BeEquivalentTo(failResult);
        }
    }

    public class CreateFileAsync : GoogleDriveServiceTests
    {
        private readonly string fileName;
        private readonly string filePath;
        private readonly string parentId;
        private readonly bool publicAccess;
        private readonly File expectedResult;

        public CreateFileAsync()
        {
            fileName = dataGenerator.Random.Word();
            filePath = Path.Combine(dataGenerator.Random.Word(), fileName);
            parentId = dataGenerator.Random.Word();
            publicAccess = dataGenerator.Random.Bool();
            expectedResult = new File();

            A.CallTo(() => googleDriveFilesService
                .GetFileAsync(fileName, parentId, cancellationToken))
                .Returns(null as File);

            A.CallTo(() => googleDriveFilesService
                .UploadFileAsync(filePath, parentId, publicAccess, cancellationToken))
                .Returns(expectedResult);
        }

        [Fact]
        public async Task ShouldCreateAndReturnFile()
        {
            var result = await service.CreateFileAsync(filePath, parentId, publicAccess, cancellationToken);
            result.Should().BeSuccess().And.HaveValue(expectedResult);
        }

        [Fact]
        public async Task ShouldReturnErrorIfAFileWithSameNameAlreadyExists()
        {
            A.CallTo(() => googleDriveFilesService
                .GetFileAsync(fileName, parentId, cancellationToken))
                .Returns(Result.Ok<File?>(new File()));

            var result = await service.CreateFileAsync(filePath, parentId, publicAccess, cancellationToken);
            result.Should().BeFailure().And.HaveError("Já existe um arquivo com o nome especificado. Se desejar sobrescrever o arquivo existente, altere a configuração GoogleDrive:RewriteExistingFile para permitir.");
        }

        [Fact]
        public async Task ShouldRepassGetFileAsyncError()
        {
            var failResult = Result.Fail("some error");
            A.CallTo(() => googleDriveFilesService
                .GetFileAsync(fileName, parentId, cancellationToken))
                .Returns(failResult);

            object result = await service.CreateFileAsync(filePath, parentId, publicAccess, cancellationToken);
            result.Should().BeEquivalentTo(failResult);
        }

        [Fact]
        public async Task ShouldRepassUploadFileAsyncError()
        {
            var failResult = Result.Fail("some error");
            A.CallTo(() => googleDriveFilesService
                .UploadFileAsync(filePath, parentId, publicAccess, cancellationToken))
                .Returns(failResult);

            object result = await service.CreateFileAsync(filePath, parentId, publicAccess, cancellationToken);
            result.Should().BeEquivalentTo(failResult);
        }
    }

    public class DeleteFileByNameAndParentNameAsync : GoogleDriveServiceTests
    {
        private readonly string fileName;
        private readonly string parentName;
        private readonly File folder;
        private readonly File file;

        public DeleteFileByNameAndParentNameAsync()
        {
            fileName = dataGenerator.Random.String();
            parentName = dataGenerator.Random.String();
            folder = new File();
            file = new File();

            A.CallTo(() => googleDriveFoldersService
                .GetFolderAsync(parentName, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(folder);

            A.CallTo(() => googleDriveFilesService
                .GetFileAsync(fileName, folder.Id, cancellationToken))
                .Returns(file);

            A.CallTo(() => googleDriveResourcesService
                .DeleteResource(file.Id, cancellationToken))
                .Returns(Result.Ok());
        }

        [Fact]
        public async Task ShouldDeleteResourceWhenFileAndFolderExists()
        {
            var result = await service.DeleteFileByNameAndParentNameAsync(fileName, parentName, cancellationToken);

            result.Should()
                .BeSuccess().And
                .Satisfy(_ => A.CallTo(() => googleDriveResourcesService
                    .DeleteResource(file.Id, cancellationToken))
                    .MustHaveHappenedOnceExactly());
        }

        [Fact]
        public async Task ShouldRepassGetFolderAsyncError()
        {
            var failResult = Result.Fail("some error");
            A.CallTo(() => googleDriveFoldersService
                .GetFolderAsync(parentName, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(failResult);

            object result = await service.DeleteFileByNameAndParentNameAsync(fileName, parentName, cancellationToken);
            result.Should().BeEquivalentTo(failResult);
        }

        [Fact]
        public async Task ShouldRepassGetFileAsyncError()
        {
            var failResult = Result.Fail("some error");
            A.CallTo(() => googleDriveFilesService
                .GetFileAsync(fileName, folder.Id, cancellationToken))
                .Returns(failResult);

            object result = await service.DeleteFileByNameAndParentNameAsync(fileName, parentName, cancellationToken);
            result.Should().BeEquivalentTo(failResult);
        }

        [Fact]
        public async Task ShouldRepassDeleteResourceError()
        {
            var failResult = Result.Fail("some error");
            A.CallTo(() => googleDriveResourcesService
                .DeleteResource(file.Id, cancellationToken))
                .Returns(failResult);

            object result = await service.DeleteFileByNameAndParentNameAsync(fileName, parentName, cancellationToken);
            result.Should().BeEquivalentTo(failResult);
        }

        [Fact]
        public async Task ShouldReturnErrorIfFolderDoesNotExists()
        {
            A.CallTo(() => googleDriveFoldersService
                .GetFolderAsync(parentName, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(Result.Ok<File?>(null));

            var result = await service.DeleteFileByNameAndParentNameAsync(fileName, parentName, cancellationToken);
            result.Should().BeFailure().And.HaveError("Não foi encontrada uma pasta com o nome especificado.");
        }

        [Fact]
        public async Task ShouldReturnErrorIfFileDoesNotExists()
        {
            A.CallTo(() => googleDriveFilesService
                .GetFileAsync(fileName, folder.Id, cancellationToken))
                .Returns(Result.Ok<File?>(null));

            var result = await service.DeleteFileByNameAndParentNameAsync(fileName, parentName, cancellationToken);
            result.Should().BeFailure().And.HaveError("Não foi encontrado um arquivo com o nome especificado.");
        }
    }


    public class SaveFilesFromLinkAsync : GoogleDriveServiceTests
    {
        private readonly string link;
        private readonly string directory;
        private readonly FileList fileList;
        private readonly List<File> files;
        private string folderId;

        public SaveFilesFromLinkAsync()
        {
            link = dataGenerator.Random.Word();
            directory = dataGenerator.Random.Word();
            folderId = dataGenerator.Random.Word();
            fileList = A.Fake<FileList>();
            files = new List<File> { new(), new(), new() };

            A.CallTo(() => extractionService
                .TryExtractGoogleDriveIdFromLink(link, out folderId))
                .Returns(true);

            A.CallTo(() => googleDriveFilesService
                .GetFilesFromFolderAsync(folderId, cancellationToken))
                .Returns(fileList);

            A.CallTo(() => fileList
                .Files)
                .Returns(files);

            A.CallTo(() => googleDriveFilesService
                .DownloadFileAsync(
                    A<File>.That.Matches(file => files.Contains(file)),
                    directory,
                    cancellationToken))
                .Returns(Result.Ok());
        }

        [Fact]
        public async Task ShouldDownloadFilesAsExpected()
        {
            var result = await service.SaveFilesFromLinkAsync(link, directory);
            result.Should().BeSuccess();
        }

        [Fact]
        public async Task ShouldCallDownloadFileOnceForEachFile()
        {
            await service.SaveFilesFromLinkAsync(link, directory);

            foreach (var file in files)
            {
                A.CallTo(() => googleDriveFilesService
                    .DownloadFileAsync(
                        file,
                        directory,
                        cancellationToken))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task ShouldReturnErrorsForEachOneReceivedFromGoogleDrive(int errorCallsQuanity)
        {
            var quantitySuccessCalls = files.Count - errorCallsQuanity;
            var failResults = files.ToDictionary(
                keySelector: file => file,
                elementSelector: _ => Result
                    .Fail(new Error(dataGenerator.Random.Word())
                    .CausedBy(dataGenerator.System.Exception())));

            var expectedErrors = failResults
                .Skip(quantitySuccessCalls)
                .SelectMany(failResult => failResult.Value.Errors);

            A.CallTo(() => googleDriveFilesService
                .DownloadFileAsync(
                    A<File>.That.Matches(file => files.Skip(quantitySuccessCalls).Contains(file)),
                    directory,
                    cancellationToken))
                .ReturnsLazily((File file, string _, CancellationToken __) => failResults[file]);

            A.CallTo(() => googleDriveFilesService
                .DownloadFileAsync(
                    A<File>.That.Matches(file => files.Take(quantitySuccessCalls).Contains(file)),
                    directory,
                    cancellationToken))
                .Returns(Result.Ok());

            var result = await service.SaveFilesFromLinkAsync(link, directory);
            result.Should()
                .BeFailure().And.Satisfy(result => result.Errors.Should()
                .BeEquivalentTo(expectedErrors));
        }

        [Fact]
        public async Task ShouldRepassGetFilesFromFolderAsyncError()
        {
            var failResult = Result.Fail("some error");
            A.CallTo(() => googleDriveFilesService
                .GetFilesFromFolderAsync(folderId, cancellationToken))
                .Returns(failResult);

            object result = await service.SaveFilesFromLinkAsync(link, directory);
            result.Should().BeEquivalentTo(failResult);
        }

        [Fact]
        public async Task ShouldReturnAFailResultIfLinkBeInvalid()
        {
            A.CallTo(() => extractionService
                .TryExtractGoogleDriveIdFromLink(link, out folderId))
                .Returns(false);

            var result = await service.SaveFilesFromLinkAsync(link, directory);
            result.Should().BeFailure().And.HaveError("O link informado é inválido.");
        }
    }

    public class ValidateFilesFromLinkAsync : GoogleDriveServiceTests
    {
        private readonly string link;
        private readonly FileList fileList;
        private readonly ValidationResult validationResult;
        private string? folderId;

        public ValidateFilesFromLinkAsync()
        {
            link = dataGenerator.Internet.Url();
            fileList = A.Fake<FileList>();
            validationResult = new ValidationResult();

            A.CallTo(() => extractionService
                .TryExtractGoogleDriveIdFromLink(link, out folderId))
                .Returns(true);

            A.CallTo(() => googleDriveFilesService
                .GetFilesFromFolderAsync(folderId!, cancellationToken))
                .Returns(fileList);

            A.CallTo(() => validator
                .ValidateAsync(fileList, cancellationToken))
                .Returns(validationResult);
        }

        [Fact]
        public async Task ShouldValidateFilesAsExpected()
        {
            var result = await service.ValidateFilesFromLinkAsync(link, cancellationToken);
            result.Should().BeSuccess();
        }

        [Fact]
        public async Task ShouldReturnAFailResultIfLinkBeInvalid()
        {
            A.CallTo(() => extractionService
                .TryExtractGoogleDriveIdFromLink(link, out folderId))
                .Returns(false);

            var result = await service.ValidateFilesFromLinkAsync(link, cancellationToken);
            result.Should().BeFailure().And.HaveError("O link informado é inválido.");
        }

        [Fact]
        public async Task ShouldRepassGetFilesFromFolderAsyncError()
        {
            var failResult = Result.Fail("Some error");
            A.CallTo(() => googleDriveFilesService
                .GetFilesFromFolderAsync(folderId!, cancellationToken))
                .Returns(failResult);

            object result = await service.ValidateFilesFromLinkAsync(link, cancellationToken);
            result.Should().BeEquivalentTo(failResult);
        }
    }

    public class GrantReaderAccessToBotFiles : GoogleDriveServiceTests
    {
        private readonly string email;
        private readonly IEnumerable<Permission> currentUserPermissions;
        private readonly Permission createUserPermission;

        public GrantReaderAccessToBotFiles()
        {
            email = dataGenerator.Person.Email;
            currentUserPermissions = new List<Permission>();
            createUserPermission = new Permission();

            A.CallTo(() => googleDrivePermissionsService
                .GetDriverAccessPermissionsAsync(email, cancellationToken))
                .Returns(Result.Ok(currentUserPermissions));

            A.CallTo(() => googleDrivePermissionsService
                .CreateBaseUserReaderPermissionAsync(email, cancellationToken))
                .Returns(createUserPermission);
        }

        [Fact]
        public async Task ShouldCreatePermissionIfUserHasNotAnyPermission()
        {
            var result = await service.GrantReaderAccessToBotFilesAsync(email, cancellationToken);

            result.Should()
                .BeSuccess().And
                .Satisfy(_ => A.CallTo(() => googleDrivePermissionsService
                    .CreateBaseUserReaderPermissionAsync(email, cancellationToken))
                    .MustHaveHappenedOnceExactly());
        }

        [Fact]
        public async Task ShouldNotCreatePermissionIfUserHasAtLeastOnePermission()
        {
            IEnumerable<Permission> currentUserPermissions = new List<Permission>() { new Permission() };
            A.CallTo(() => googleDrivePermissionsService
                .GetDriverAccessPermissionsAsync(email, cancellationToken))
                .Returns(Result.Ok(currentUserPermissions));

            var result = await service.GrantReaderAccessToBotFilesAsync(email, cancellationToken);

            result.Should()
                .BeSuccess().And
                .Satisfy(_ => A.CallTo(() => googleDrivePermissionsService
                    .CreateBaseUserReaderPermissionAsync(email, cancellationToken))
                    .MustNotHaveHappened());
        }

        [Fact]
        public async Task ShouldRepassGetDriverAccessPermissionsAsyncError()
        {
            var failResult = Result.Fail("some error");
            A.CallTo(() => googleDrivePermissionsService
                .GetDriverAccessPermissionsAsync(email, cancellationToken))
                .Returns(failResult);

            object result = await service.GrantReaderAccessToBotFilesAsync(email, cancellationToken);
            result.Should().BeEquivalentTo(failResult);
        }

        [Fact]
        public async Task ShouldRepassCreateBaseUserReaderPermissionAsyncError()
        {
            var failResult = Result.Fail("some error");
            A.CallTo(() => googleDrivePermissionsService
                .CreateBaseUserReaderPermissionAsync(email, cancellationToken))
                .Returns(failResult);

            object result = await service.GrantReaderAccessToBotFilesAsync(email, cancellationToken);
            result.Should().BeEquivalentTo(failResult);
        }
    }

    public class RevokeReaderAccessToBotFilesAsync : GoogleDriveServiceTests
    {
        private readonly string email;
        private readonly IEnumerable<Permission> currentUserPermissions;

        public RevokeReaderAccessToBotFilesAsync()
        {
            email = dataGenerator.Person.Email;
            currentUserPermissions = new List<Permission>();

            A.CallTo(() => googleDrivePermissionsService
                .GetDriverAccessPermissionsAsync(email, cancellationToken))
                .Returns(Result.Ok(currentUserPermissions));

            A.CallTo(() => googleDrivePermissionsService
                .DeleteBaseUserPermissionsAsync(currentUserPermissions, cancellationToken))
                .Returns(Result.Ok());
        }

        [Fact]
        public async Task ShouldRevokePermissionSuccessfuly()
        {
            var result = await service.RevokeReaderAccessToBotFilesAsync(email, cancellationToken);

            result.Should()
                .BeSuccess().And
                .Satisfy(_ => A.CallTo(() => googleDrivePermissionsService
                    .DeleteBaseUserPermissionsAsync(currentUserPermissions, cancellationToken))
                    .MustHaveHappenedOnceExactly());
        }

        [Fact]
        public async Task ShouldRepassGetDriverAccessPermissionsAsyncError()
        {
            var failResult = Result.Fail("some error");
            A.CallTo(() => googleDrivePermissionsService
                .GetDriverAccessPermissionsAsync(email, cancellationToken))
                .Returns(failResult);

            object result = await service.RevokeReaderAccessToBotFilesAsync(email, cancellationToken);
            result.Should().BeEquivalentTo(failResult);
        }

        [Fact]
        public async Task ShouldDeleteBaseUserPermissionsAsyncError()
        {
            var failResult = Result.Fail("some error");
            A.CallTo(() => googleDrivePermissionsService
                .DeleteBaseUserPermissionsAsync(currentUserPermissions, cancellationToken))
                .Returns(failResult);

            var result = await service.RevokeReaderAccessToBotFilesAsync(email, cancellationToken);
            result.Should().BeSameAs(failResult);
        }
    }
}
