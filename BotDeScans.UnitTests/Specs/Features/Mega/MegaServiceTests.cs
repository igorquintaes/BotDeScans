using AutoFixture;
using BotDeScans.App.Features.Mega;
using BotDeScans.App.Features.Mega.InternalServices;
using BotDeScans.UnitTests.Specs.Extensions;
using CG.Web.MegaApiClient;
using FakeItEasy;
using FluentAssertions;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Xunit;
namespace BotDeScans.UnitTests.Specs.Features.Mega;

public class MegaServiceTests : UnitTest
{
    private readonly MegaService service;

    public MegaServiceTests()
    {
        fixture.Fake<MegaFilesService>();
        fixture.Fake<MegaFoldersService>();
        fixture.Fake<IConfiguration>();

        service = fixture.Create<MegaService>();
    }

    public class GetOrCreateFolderAsync : MegaServiceTests
    {
        public GetOrCreateFolderAsync()
        {
            A.CallTo(() => fixture
                .Fake<MegaFoldersService>()
                .GetAsync(A<string>.Ignored, A<INode>.Ignored))
                .Returns(Result.Ok(null as INode));

            A.CallTo(() => fixture
                .Fake<MegaFoldersService>()
                .CreateAsync(A<string>.Ignored, A<INode>.Ignored))
                .Returns(fixture.Fake<INode>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionForNewFolderShouldReturnSuccessResultAndReturnCreatedFolder()
        {
            var result = await service.GetOrCreateFolderAsync(fixture.Create<string>(), fixture.Create<INode>());

            result.Should().BeSuccess().And.HaveValue(fixture.Fake<INode>());
        }

        [Fact]
        public async Task GivenSuccessfullExecutionForExistingFolderShouldReturnSuccessResultAndExistingFolder()
        {
            var folderName = fixture.Create<string>();
            var parentNode = fixture.Create<INode>();
            var existingNode = fixture.Create<INode>();

            A.CallTo(() => fixture
                .Fake<MegaFoldersService>()
                .GetAsync(folderName, parentNode))
                .Returns(Result.Ok<INode?>(existingNode));

            var result = await service.GetOrCreateFolderAsync(folderName, parentNode);

            result.Should().BeSuccess().And.HaveValue(existingNode);

            A.CallTo(() => fixture
                .Fake<MegaFoldersService>()
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
                .Fake<MegaFoldersService>()
                .GetAsync(folderName, parentNode))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.GetOrCreateFolderAsync(folderName, parentNode);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
