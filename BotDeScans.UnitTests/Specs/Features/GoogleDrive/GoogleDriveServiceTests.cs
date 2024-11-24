﻿using AutoFixture;
using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services;
using BotDeScans.UnitTests.Specs.Extensions;
using BotDeScans.UnitTests.Specs.Services;
using FakeItEasy;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Google.Apis.Drive.v3.Data;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
namespace BotDeScans.UnitTests.Specs.Features.GoogleDrive.InternalServices;

public class GoogleDriveServiceTests : UnitTest
{
    private readonly GoogleDriveService service;

    public GoogleDriveServiceTests()
    {
        fixture.Fake<GoogleDriveFilesService>();
        fixture.Fake<GoogleDriveFoldersService>();
        fixture.Fake<GoogleDriveResourcesService>();
        fixture.Fake<GoogleDrivePermissionsService>();
        fixture.Fake<IValidator<IList<File>>>();
        fixture.Fake<IConfiguration>();

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
                .Fake<GoogleDriveFoldersService>()
                .GetAsync(folderName, parentId, cancellationToken))
                .Returns(Result.Ok<File?>(default));

            A.CallTo(() => fixture
                .Fake<GoogleDriveFoldersService>()
                .CreateAsync(folderName, parentId, cancellationToken))
                .Returns(fixture.Fake<File>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionForNewFolderShouldReturnSuccessResultAndReturnCreatedFolder()
        {
            var result = await service.GetOrCreateFolderAsync(folderName, parentId, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Fake<File>());

            A.CallTo(() => fixture
                .Fake<GoogleDriveFoldersService>()
                .CreateAsync(folderName, parentId, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfullExecutionForExistingFolderShouldReturnSuccessResultAndExistingFolder()
        {
            A.CallTo(() => fixture
                .Fake<GoogleDriveFoldersService>()
                .GetAsync(folderName, parentId, cancellationToken))
                .Returns(fixture.Fake<File>());

            var result = await service.GetOrCreateFolderAsync(folderName, parentId, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Fake<File>());

            A.CallTo(() => fixture
                .Fake<GoogleDriveFoldersService>()
                .CreateAsync(folderName, parentId, cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenErrorWhileCheckingIfFolderExistsShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .Fake<GoogleDriveFoldersService>()
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
                .Fake<GoogleDriveFoldersService>()
                .CreateAsync(folderName, parentId, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.GetOrCreateFolderAsync(folderName, parentId, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }

    public class CreateFileAsync : GoogleDriveServiceTests
    {
        private static string filePath = System.IO.Path.Combine("directory", "file.zip");
        private static string fileName = "file.zip";

        private static string parentId;
        private static bool publicAccess;

        public CreateFileAsync()
        {
            parentId = fixture.Create<string>();
            publicAccess = fixture.Create<bool>();

            A.CallTo(() => fixture
                .Fake<GoogleDriveFilesService>()
                .GetAsync(fileName, parentId, cancellationToken))
                .Returns(null as File);

            A.CallTo(() => fixture
                .Fake<GoogleDriveFilesService>()
                .UploadAsync(filePath, parentId, publicAccess, cancellationToken))
                .Returns(fixture.Fake<File>());

            A.CallTo(() => fixture
                .Fake<GoogleDriveFilesService>()
                .UpdateAsync(filePath, fixture.Fake<File>().Id, cancellationToken))
                .Returns(fixture.Fake<File>());

            A.CallTo(() => fixture
                .Fake<IConfiguration>()
                .GetSection(GoogleDriveService.REWRITE_KEY))
                .Returns(fixture.Fake<IConfigurationSection>());

            A.CallTo(() => fixture
                .Fake<IConfigurationSection>().Value)
                .Returns("true");
        }

        [Fact]
        public async Task GivenExecutionSuccessfulForANewFileShouldReturnSuccessResultAndNewFileValue()
        {
            var result = await service.CreateFileAsync(filePath, parentId, publicAccess, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Fake<File>());

            A.CallTo(() => fixture
                .Fake<GoogleDriveFilesService>()
                .UploadAsync(filePath, parentId, publicAccess, cancellationToken))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .Fake<GoogleDriveFilesService>()
                .UpdateAsync(A<string>.Ignored, A<string>.Ignored, cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenExecutionSuccessfulForAnExistingFileWithRewriteShouldReturnSuccessResultAndUpdatedFileValue()
        {
            A.CallTo(() => fixture
                .Fake<GoogleDriveFilesService>()
                .GetAsync(fileName, parentId, cancellationToken))
                .Returns(fixture.Fake<File>());

            var result = await service.CreateFileAsync(filePath, parentId, publicAccess, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Fake<File>());

            A.CallTo(() => fixture
                .Fake<GoogleDriveFilesService>()
                .UpdateAsync(filePath, fixture.Fake<File>().Id, cancellationToken))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .Fake<GoogleDriveFilesService>()
                .UploadAsync(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored, cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenExistingFileAndNotAllowedToRewriteShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .Fake<GoogleDriveFilesService>()
                .GetAsync(fileName, parentId, cancellationToken))
                .Returns(fixture.Fake<File>());

            A.CallTo(() => fixture
                .Fake<IConfigurationSection>().Value)
                .Returns("false");

            var result = await service.CreateFileAsync(filePath, parentId, publicAccess, cancellationToken);

            result.Should().BeFailure().And.HaveError($"Já existe um arquivo com o nome especificado. Se desejar sobrescrever o arquivo existente, altere a configuração {GoogleDriveService.REWRITE_KEY} para permitir.");
        }

        [Fact]
        public async Task GivenExistingFileAndNotSpecifiedToRewriteShouldNotAllowActionAndReturnFailResult()
        {
            A.CallTo(() => fixture
                .Fake<GoogleDriveFilesService>()
                .GetAsync(fileName, parentId, cancellationToken))
                .Returns(fixture.Fake<File>());

            A.CallTo(() => fixture
                .Fake<IConfigurationSection>().Value)
                .Returns(null as string);

            var result = await service.CreateFileAsync(filePath, parentId, publicAccess, cancellationToken);

            result.Should().BeFailure().And.HaveError($"Já existe um arquivo com o nome especificado. Se desejar sobrescrever o arquivo existente, altere a configuração {GoogleDriveService.REWRITE_KEY} para permitir.");
        }

        [Fact]
        public async Task GivenGetFileErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .Fake<GoogleDriveFilesService>()
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
                .Fake<GoogleDriveFilesService>()
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
                .Fake<GoogleDriveFilesService>()
                .GetAsync(fileName, parentId, cancellationToken))
                .Returns(fixture.Fake<File>());

            A.CallTo(() => fixture
                .Fake<GoogleDriveFilesService>()
                .UpdateAsync(filePath, fixture.Fake<File>().Id, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.CreateFileAsync(filePath, parentId, publicAccess, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }

    public class DeleteFileByNameAndParentNameAsync : GoogleDriveServiceTests
    {
        private static string fileName;
        private static string parentFolderName;
        private static File[] resources;

        public DeleteFileByNameAndParentNameAsync()
        {
            GoogleDriveSettingsService.BaseFolderId = fixture.Create<string>();
            fileName = fixture.Create<string>();
            parentFolderName = fixture.Create<string>();
            resources = fixture.Fake<File>(2);

            A.CallTo(() => fixture
                .Fake<GoogleDriveFoldersService>()
                .GetAsync(parentFolderName, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(resources[0]);

            A.CallTo(() => fixture
                .Fake<GoogleDriveFilesService>()
                .GetAsync(fileName, resources[0].Id, cancellationToken))
                .Returns(resources[1]);

            A.CallTo(() => fixture
                .Fake<GoogleDriveResourcesService>()
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
                .Fake<GoogleDriveFoldersService>()
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
                .Fake<GoogleDriveFoldersService>()
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
                .Fake<GoogleDriveFilesService>()
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
                .Fake<GoogleDriveFilesService>()
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
                .Fake<GoogleDriveResourcesService>()
                .DeleteResource(resources[1].Id, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.DeleteFileByNameAndParentNameAsync(fileName, parentFolderName, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }

    public class GetFolderIdFromUrl : GoogleDriveServiceTests
    {
        [Theory]
        [InlineData("https://drive.google.com/drive/folders/1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn?usp=sharing")]
        [InlineData("https://drive.google.com/drive/folders/1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn")]
        [InlineData("https://drive.google.com/folderview?id=1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn")]
        [InlineData("https://drive.google.com/open?id=1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn")]
        public void IsValid(string url) => 
            service.GetFolderIdFromUrl(url).Should().BeSuccess().And.HaveValue("1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn");

        [Theory]
        [InlineData(null)]
        [InlineData("not a valid url")]
        [InlineData("https://random.drive.google.com/1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn")]
        [InlineData("https://drive.google.com.random/1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn")]
        [InlineData("https://drive.google.com/drive/folders/")]
        [InlineData("https://drive.google.com/drive/folders/randomValue")]
        [InlineData("https://drive.google.com/folderview")]
        [InlineData("https://drive.google.com/folderview?id=")]
        [InlineData("https://drive.google.com/folderview?id=randomValue")]
        public void IsInvalid(string url) =>
            service.GetFolderIdFromUrl(url).Should().BeFailure().And.HaveError("O link informado é inválido.");
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
                .Fake<GoogleDriveFilesService>()
                .GetManyAsync(folderId, cancellationToken))
                .Returns(fixture.Fake<File>(2));

            A.CallTo(() => fixture
                .Fake<GoogleDriveFilesService>()
                .DownloadAsync(
                    A<File>.That.Matches(file => fixture.Fake<File[]>().Contains(file)), 
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
                .Fake<GoogleDriveFilesService>()
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
                .Fake<GoogleDriveFilesService>()
                .DownloadAsync(
                    A<File>.That.Matches(file => fixture.Fake<File[]>().Contains(file)),
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

    public class ValidateFilesAsync : GoogleDriveServiceTests
    {
        private readonly string folderId;

        public ValidateFilesAsync()
        {
            folderId = fixture.Create<string>();

            A.CallTo(() => fixture
                .Fake<GoogleDriveFilesService>()
                .GetManyAsync(folderId, cancellationToken))
                .Returns(Result.Ok(fixture.Fake<IList<File>>()));

            A.CallTo(() => fixture
                .Fake<IValidator<IList<File>>>()
                .ValidateAsync(fixture.Fake<IList<File>>(), cancellationToken))
                .Returns(new ValidationResult());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await service.ValidateFilesAsync(folderId, cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenErrorWhenObtainingFilesShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .Fake<GoogleDriveFilesService>()
                .GetManyAsync(folderId, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.ValidateFilesAsync(folderId, cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenAnyValidationErrorsShouldReturnFailResultWithErrorsMessages()
        {
            const string FIRST_ERROR_MESSAGE = "some error";
            const string SECOND_ERROR_MESSAGE = "other error";

            A.CallTo(() => fixture
                .Fake<IValidator<IList<File>>>()
                .ValidateAsync(fixture.Fake<IList<File>>(), cancellationToken))
                .Returns(new ValidationResult(new[] { 
                    new ValidationFailure("1", FIRST_ERROR_MESSAGE),
                    new ValidationFailure("2", SECOND_ERROR_MESSAGE) 
                }));

            var result = await service.ValidateFilesAsync(folderId, cancellationToken);
            result.Should().BeFailure().And
                .HaveError(FIRST_ERROR_MESSAGE).And
                .HaveError(SECOND_ERROR_MESSAGE);
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
                .Fake<GoogleDrivePermissionsService>()
                .GetUserPermissionsAsync(email, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(Result.Ok<IEnumerable<Permission>>([]));

            A.CallTo(() => fixture
                .Fake<GoogleDrivePermissionsService>()
                .CreateUserReaderPermissionAsync(email, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(fixture.Fake<Permission>());
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
                .Fake<GoogleDrivePermissionsService>()
                .GetUserPermissionsAsync(email, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(Result.Ok<IEnumerable<Permission>>([new Permission()]));

            var result = await service.GrantReaderAccessToBotFilesAsync(email, cancellationToken);

            result.Should().BeSuccess();

            A.CallTo(() => fixture
                .Fake<GoogleDrivePermissionsService>()
                .CreateUserReaderPermissionAsync(A<string>.Ignored, A<string>.Ignored, cancellationToken))
                .MustNotHaveHappened();
        }
        
        [Fact]
        public async Task GivenErrorToObtainPermissionsShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .Fake<GoogleDrivePermissionsService>()
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
                .Fake<GoogleDrivePermissionsService>()
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
                .Fake<GoogleDrivePermissionsService>()
                .GetUserPermissionsAsync(email, GoogleDriveSettingsService.BaseFolderId, cancellationToken))
                .Returns(Result.Ok(fixture.Fake<IEnumerable<Permission>>()));

            A.CallTo(() => fixture
                .Fake<GoogleDrivePermissionsService>()
                .DeleteUserReaderPermissionsAsync(
                    fixture.Fake<IEnumerable<Permission>>(), 
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
                .Fake<GoogleDrivePermissionsService>()
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
                .Fake<GoogleDrivePermissionsService>()
                .DeleteUserReaderPermissionsAsync(
                    fixture.Fake<IEnumerable<Permission>>(), 
                    GoogleDriveSettingsService.BaseFolderId, 
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.RevokeReaderAccessToBotFilesAsync(email, cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}