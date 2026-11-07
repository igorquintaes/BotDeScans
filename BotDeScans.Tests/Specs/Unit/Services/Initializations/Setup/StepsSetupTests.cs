using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services.Initializations.Setups;

namespace BotDeScans.Tests.Specs.Services.Initializations.Setup;

public class StepsSetupTests : UnitTest
{
    public class Run : StepsSetupTests
    {
        private readonly StepsSetup setup;
        private readonly IStep step;

        public Run()
        {
            step = A.Fake<IStep>();

            A.CallTo(() => step.Name).Returns(StepName.UploadMangadex);
            A.CallTo(() => step.Type).Returns(StepType.Upload);

            fixture.FreezeFakeConfiguration(StepsSetup.STEPS_KEY, [step.Name.ToString()]);

            fixture.Inject<IEnumerable<IStep>>([step]);

            setup = fixture.Create<StepsSetup>();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await setup.ExecuteAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenEmptyStepsKeysShouldReturnFailResult()
        {

            fixture.FreezeFakeConfiguration(StepsSetup.STEPS_KEY, []);

            var result = await setup.ExecuteAsync(cancellationToken);

            var errorMessage = "Não foi encontrado nenhum passo de publicação " +
                              $"em '{StepsSetup.STEPS_KEY}'.";
            result.Should().BeFailure().And.HaveError(errorMessage);
        }

        [Fact]
        public async Task GivenSingleStepKeyWithUnexpectedValueShouldReturnFailResult()
        {
            const string UNEXPECTED_STEP_KEY = "unexpected";

            fixture.FreezeFakeConfiguration(StepsSetup.STEPS_KEY, [UNEXPECTED_STEP_KEY]);

            var result = await setup.ExecuteAsync(cancellationToken);

            var errorMessage = $"{StepsSetup.STEPS_KEY}: Não foi possível converter o tipo " +
                               $"'{UNEXPECTED_STEP_KEY}' em um passo de publicação válido.";
            result.Should().BeFailure().And.HaveError(errorMessage);
        }

        [Fact]
        public async Task GivenSomeUnexpectedStepKeyShouldReturnFailResult()
        {
            const string UNEXPECTED_STEP_KEY = "unexpected";

            fixture.FreezeFakeConfiguration(
                StepsSetup.STEPS_KEY,
                [UNEXPECTED_STEP_KEY, StepName.UploadMangadex.ToString()]);

            var result = await setup.ExecuteAsync(cancellationToken);

            var errorMessage = $"{StepsSetup.STEPS_KEY}: Não foi possível converter o tipo " +
                               $"'{UNEXPECTED_STEP_KEY}' em um passo de publicação válido.";
            result.Should().BeFailure().And.HaveError(errorMessage);
        }

        [Fact]
        public async Task GivenStepsWithoutAnyUploadTypeShouldReturnFailResult()
        {
            var stepA = A.Fake<IStep>();
            var stepB = A.Fake<IStep>();
            var stepC = A.Fake<IStep>();

            A.CallTo(() => stepA.Name).Returns(StepName.Download);
            A.CallTo(() => stepA.Type).Returns(StepType.Management);

            A.CallTo(() => stepB.Name).Returns(StepName.PdfFiles);
            A.CallTo(() => stepB.Type).Returns(StepType.Conversion);

            A.CallTo(() => stepC.Name).Returns(StepName.PublishBlogspot);
            A.CallTo(() => stepC.Type).Returns(StepType.Publish);

            fixture.FreezeFakeConfiguration(
                StepsSetup.STEPS_KEY,
                [
                    stepA.Name.ToString(),
                    stepB.Name.ToString(),
                    stepC.Name.ToString()
                ]);

            var result = await setup.ExecuteAsync(cancellationToken);

            var errorMessage = "Não foi encontrado nenhum passo de upload de " +
                              $"lançamentos em '{StepsSetup.STEPS_KEY}'.";
            result.Should().BeFailure().And.HaveError(errorMessage);
        }
    }
}
