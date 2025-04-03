using BotDeScans.App.Features.Mega;
using BotDeScans.App.Features.Mega.InternalServices;
using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.Steps;
using CG.Web.MegaApiClient;
using FluentResults;
namespace BotDeScans.UnitTests.Specs.Features.Publish.Steps;

public class UploadPdfMegaStepTests : UnitTest
{
    private readonly IStep step;

    public UploadPdfMegaStepTests()
    {
        fixture.Freeze<PublishState>();
        fixture.FreezeFake<IServiceProvider>();
        step = fixture.Create<UploadPdfMegaStep>();
    }

    public class Properties : UploadPdfMegaStepTests
    {
        [Fact]
        public void ShouldHaveExpectedName() =>
            step.StepName.Should().Be(StepEnum.UploadPdfMega);

        [Fact]
        public void ShouldHaveExpectedType() =>
            step.StepType.Should().Be(StepType.Publish);
    }

    public class ValidateBeforeFilesManagementAsync : UploadPdfMegaStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateBeforeFilesManagementAsync(cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ValidateAfterFilesManagementAsync : UploadPdfMegaStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateAfterFilesManagementAsync(cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ExecuteAsync : UploadPdfMegaStepTests
    {
        private const string FILE_LINK = "http://www.escoladescans.com/sample";

        public ExecuteAsync()
        {
            var rootNode = A.Fake<INode>();
            var titleFolderNode = A.Fake<INode>();

            A.CallTo(() => fixture
                .FreezeFake<IServiceProvider>()
                .GetService(typeof(MegaService)))
                .Returns(fixture.FreezeFake<MegaService>());

            A.CallTo(() => fixture
                .FreezeFake<IServiceProvider>()
                .GetService(typeof(MegaSettingsService)))
                .Returns(fixture.FreezeFake<MegaSettingsService>());

            A.CallTo(() => fixture
                .FreezeFake<MegaSettingsService>()
                .GetRootFolderAsync())
                .Returns(rootNode);

            A.CallTo(() => fixture
                .FreezeFake<MegaService>()
                .GetOrCreateFolderAsync(fixture.Freeze<PublishState>().Title.Name, rootNode))
                .Returns(Result.Ok(titleFolderNode));

            A.CallTo(() => fixture
                .FreezeFake<MegaService>()
                .CreateFileAsync(
                    fixture.Freeze<PublishState>().InternalData.PdfFilePath,
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
        public async Task GivenSuccessfulExecutionShouldSetMegaPdfStateValue()
        {
            fixture.Freeze<PublishState>().ReleaseLinks.MegaPdf = null!;

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<PublishState>().ReleaseLinks.MegaPdf.Should().Be(FILE_LINK);
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
