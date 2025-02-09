using AutoFixture;
using BotDeScans.App.Features.Mega;
using BotDeScans.App.Features.Mega.InternalServices;
using BotDeScans.UnitTests.Extensions;
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
namespace BotDeScans.UnitTests.Specs.Features.Mega;

public class MegaServiceTests : UnitTest
{
    private readonly MegaService service;

    public MegaServiceTests()
    {
        fixture.FreezeFake<MegaFilesService>();
        fixture.FreezeFake<MegaFoldersService>();
        fixture.FreezeFake<IConfiguration>();

        service = fixture.Create<MegaService>();
    }

    public class GetOrCreateFolderAsync : MegaServiceTests
    {
        public GetOrCreateFolderAsync()
        {
            A.CallTo(() => fixture
                .FreezeFake<MegaFoldersService>()
                .GetAsync(A<string>.Ignored, A<INode>.Ignored))
                .Returns(Result.Ok(null as INode));

            A.CallTo(() => fixture
                .FreezeFake<MegaFoldersService>()
                .CreateAsync(A<string>.Ignored, A<INode>.Ignored))
                .Returns(fixture.FreezeFake<INode>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionForNewFolderShouldReturnSuccessResultAndReturnCreatedFolder()
        {
            var result = await service.GetOrCreateFolderAsync(fixture.Create<string>(), fixture.Create<INode>());

            result.Should().BeSuccess().And.HaveValue(fixture.FreezeFake<INode>());
        }

        [Fact]
        public async Task GivenSuccessfullExecutionForExistingFolderShouldReturnSuccessResultAndExistingFolder()
        {
            var folderName = fixture.Create<string>();
            var parentNode = fixture.Create<INode>();
            var existingNode = fixture.Create<INode>();

            A.CallTo(() => fixture
                .FreezeFake<MegaFoldersService>()
                .GetAsync(folderName, parentNode))
                .Returns(Result.Ok<INode?>(existingNode));

            var result = await service.GetOrCreateFolderAsync(folderName, parentNode);

            result.Should().BeSuccess().And.HaveValue(existingNode);

            A.CallTo(() => fixture
                .FreezeFake<MegaFoldersService>()
                .CreateAsync(folderName, parentNode))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenErrorWhileCheckingIfFolderExistsShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";
            var folderName = fixture.Create<string>();
            var parentNode = fixture.Create<INode>();

            A.CallTo(() => fixture
                .FreezeFake<MegaFoldersService>()
                .GetAsync(folderName, parentNode))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.GetOrCreateFolderAsync(folderName, parentNode);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }

    public class CreateFileAsync : MegaServiceTests
    {
        private readonly string filePath = Path.Combine("directory", "file.zip");
        private readonly string fileName = "file.zip";

        private readonly INode parent;

        public CreateFileAsync()
        {
            parent = fixture.Create<INode>();

            A.CallTo(() => fixture
                .FreezeFake<MegaFilesService>()
                .GetAsync(fileName, parent))
                .Returns(Result.Ok(null as INode));

            A.CallTo(() => fixture
                .FreezeFake<MegaFilesService>()
                .UploadAsync(filePath, parent, cancellationToken))
                .Returns(fixture.FreezeFake<Uri>());

            fixture.FreezeFakeConfiguration(MegaService.REWRITE_KEY, "true");
        }

        [Fact]
        public async Task GivenExecutionSuccessfulForANewFileShouldReturnSuccessResultAndUriValue()
        {
            var result = await service.CreateFileAsync(filePath, parent, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.FreezeFake<Uri>());

            A.CallTo(() => fixture
                .FreezeFake<MegaFilesService>()
                .UploadAsync(filePath, parent, cancellationToken))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<MegaFilesService>()
                .DeleteAsync(A<INode>.Ignored))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenExecutionSuccessfulForAnExistingFileWithRewriteShouldReturnSuccessResultAndDeleteFileValue()
        {
            A.CallTo(() => fixture
                .FreezeFake<MegaFilesService>()
                .GetAsync(fileName, parent))
                .Returns(Result.Ok<INode?>(fixture.FreezeFake<INode>()));

            var result = await service.CreateFileAsync(filePath, parent, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.FreezeFake<Uri>());

            A.CallTo(() => fixture
                .FreezeFake<MegaFilesService>()
                .DeleteAsync(fixture.FreezeFake<INode>()))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<MegaFilesService>()
                .UploadAsync(filePath, parent, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenExistingFileAndNotAllowedToRewriteShouldReturnFailResult()
        {
            fixture.FreezeFakeConfiguration(MegaService.REWRITE_KEY, "false");

            A.CallTo(() => fixture
                .FreezeFake<MegaFilesService>()
                .GetAsync(fileName, parent))
                .Returns(Result.Ok<INode?>(fixture.FreezeFake<INode>()));

            var result = await service.CreateFileAsync(filePath, parent, cancellationToken);

            result.Should().BeFailure().And.HaveError($"Já existe um arquivo com o nome especificado. Se desejar sobrescrever o arquivo existente, altere a configuração {MegaService.REWRITE_KEY} para permitir.");
        }

        [Fact]
        public async Task GivenExistingFileAndNotSpecifiedToRewriteShouldNotAllowActionAndReturnFailResult()
        {
            fixture.FreezeFakeConfiguration(MegaService.REWRITE_KEY, null as string);

            A.CallTo(() => fixture
                .FreezeFake<MegaFilesService>()
                .GetAsync(fileName, parent))
                .Returns(Result.Ok<INode?>(fixture.FreezeFake<INode>()));

            var result = await service.CreateFileAsync(filePath, parent, cancellationToken);

            result.Should().BeFailure().And.HaveError($"Já existe um arquivo com o nome especificado. Se desejar sobrescrever o arquivo existente, altere a configuração {MegaService.REWRITE_KEY} para permitir.");
        }

        [Fact]
        public async Task GivenGetFileErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<MegaFilesService>()
                .GetAsync(fileName, parent))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.CreateFileAsync(filePath, parent, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
