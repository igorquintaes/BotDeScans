using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services.Initializations;

namespace BotDeScans.UnitTests.Specs.Services;

public class SetupStepsServiceTests : UnitTest
{
    private readonly SetupStepsService service;

    public SetupStepsServiceTests()
    {
        var mangaDexStep = A.Fake<UploadMangaDexStep>();
        var bloggerStep = A.Fake<PublishBloggerStep>();

        fixture.Inject<IEnumerable<IStep>>([mangaDexStep, bloggerStep]);
        fixture.FreezeFakeConfiguration(
            SetupStepsService.STEPS_KEY,
            [mangaDexStep.Name.ToString(), bloggerStep.Name.ToString()]);

        service = fixture.Create<SetupStepsService>();
    }

    public class Setup : SetupStepsServiceTests
    {
        [Fact]
        public void GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = service.Setup();
            result.Should().BeSuccess();
        }

        [Fact]
        public void GivenNoneStepsFoundInConfigurationShouldReturnFailResult()
        {
            fixture.FreezeFakeConfiguration(SetupStepsService.STEPS_KEY, []);

            var result = service.Setup();
            result.Should().BeFailure().And.HaveError($"Não foi encontrado nenhum passo de publicação em '{SetupStepsService.STEPS_KEY}'.");
        }

        [Fact]
        public void GivenErrorToParseSomeStepShouldReturnFailResult()
        {
            var validStep = StepName.UploadMangadex.ToString();
            var invalidStep = fixture.Create<string>();
            fixture.FreezeFakeConfiguration(SetupStepsService.STEPS_KEY, [validStep, invalidStep]);

            var result = service.Setup();
            result.Should().BeFailure().And.HaveError($"{SetupStepsService.STEPS_KEY}: Não foi possível converter o tipo '{invalidStep}' em um passo de publicação válido.");
        }

        [Fact]
        public void GivenNoneUploadConfiguredStepShouldReturnFailResult()
        {
            var bloggerStep = A.Fake<PublishBloggerStep>();
            fixture.Inject<IEnumerable<IStep>>([bloggerStep]);

            var result = fixture.Create<SetupStepsService>().Setup();
            result.Should().BeFailure().And.HaveError($"Não foi encontrado nenhum passo de upload de lançamentos em '{SetupStepsService.STEPS_KEY}'.");
        }
    }
}
