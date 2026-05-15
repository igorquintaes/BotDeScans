using BotDeScans.App.Features.Mega;
using BotDeScans.App.Features.Mega.InternalServices;
using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;
using CG.Web.MegaApiClient;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class UploadZipMegaStepTests : UnitTest
{
    private readonly UploadZipMegaStep step;

    public UploadZipMegaStepTests()
    {
        fixture.FreezeFake<IPublishContext>();
        fixture.FreezeFake<MegaService>();
        fixture.FreezeFake<MegaSettingsService>();
        step = fixture.Create<UploadZipMegaStep>();
    }

    public class Properties : UploadZipMegaStepTests
    {
        [Fact]
        public void ShouldHaveExpectedType() =>
            step.Type.Should().Be(StepType.Upload);

        [Fact]
        public void ShouldHaveExpectedName() =>
            step.Name.Should().Be(StepName.UploadZipMega);

        [Fact]
        public void ShouldHaveExpectedDependency() =>
            step.Dependency.Should().Be(StepName.ZipFiles);
    }

    public class ValidateAsync : UploadZipMegaStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateAsync(cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ExecuteAsync : UploadZipMegaStepTests
    {
        private const string FILE_LINK = "http://www.escoladescans.com/sample";

        public ExecuteAsync()
        {
            var rootNode = A.Fake<INode>();
            var titleFolderNode = A.Fake<INode>();
            var title = fixture.Create<Title>();
            var zipPath = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<IPublishContext>().Title)
                .Returns(title);

            A.CallTo(() => fixture
                .FreezeFake<IPublishContext>().ZipFilePath)
                .Returns(zipPath);

            A.CallTo(() => fixture
                .FreezeFake<MegaSettingsService>()
                .GetRootFolderAsync())
                .Returns(rootNode);

            A.CallTo(() => fixture
                .FreezeFake<MegaService>()
                .GetOrCreateFolderAsync(title.Name, rootNode))
                .Returns(Result.Ok(titleFolderNode));

            A.CallTo(() => fixture
                .FreezeFake<MegaService>()
                .CreateFileAsync(
                    zipPath,
                    titleFolderNode,
                    cancellationToken))
                .Returns(Result.Ok(new Uri(FILE_LINK)));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSetMegaZipContextValue()
        {
            await step.ExecuteAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IPublishContext>()
                .SetMegaZipLink(FILE_LINK))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorToGetOrCreateFolderShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<MegaService>()
                .GetOrCreateFolderAsync(A<string>.Ignored, A<INode>.Ignored))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToCreateFileShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<MegaService>()
                .CreateFileAsync(
                    A<string>.Ignored,
                    A<INode>.Ignored,
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
