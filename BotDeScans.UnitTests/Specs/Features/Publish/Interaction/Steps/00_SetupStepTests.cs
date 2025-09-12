using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Pings;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;
using FluentValidation;
using FluentValidation.Results;
namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class SetupStepTests : UnitTest
{
    private readonly SetupStep step;

    public SetupStepTests()
    {
        fixture.Freeze<State>();
        fixture.FreezeFake<TitleRepository>();
        fixture.FreezeFake<IValidator<State>>();
        fixture.Inject<IEnumerable<Ping>>([fixture.FreezeFake<Ping>()]);

        step = fixture.Create<SetupStep>();
    }

    public class Properties : SetupStepTests
    {
        [Fact]
        public void ShouldHaveExpectedType() =>
            step.Type.Should().Be(StepType.Management);

        [Fact]
        public void ShouldHaveExpectedName() =>
            step.Name.Should().Be(StepName.Setup);

        [Fact]
        public void ShouldHaveExpectedIsMandatory() =>
            step.IsMandatory.Should().Be(true);
    }

    public class ExecuteAsync : SetupStepTests
    {
        public ExecuteAsync()
        {
            A.CallTo(() => fixture
                .FreezeFake<IValidator<State>>()
                .ValidateAsync(fixture.Freeze<State>(), cancellationToken))
                .Returns(new ValidationResult());

            A.CallTo(() => fixture
                .FreezeFake<Ping>().IsApplicable)
                .Returns(true);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSetPublishStateTitle()
        {
            var title = fixture
                .Build<Title>()
                .With(x => x.Id, fixture.Freeze<State>().ChapterInfo.TitleId)
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(title.Id, cancellationToken))
                .Returns(title);

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<State>().Title.Should().Be(title);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSetPing()
        {
            var ping = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<Ping>()
                .GetPingAsTextAsync(cancellationToken))
                .Returns(ping);

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<State>().InternalData.Pings.Should().Be(ping);
        }

        [Fact]
        public async Task GivenValidationErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<IValidator<State>>()
                .ValidateAsync(fixture.Freeze<State>(), cancellationToken))
                .Returns(new ValidationResult([new ValidationFailure("prop", ERROR_MESSAGE)]));

            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}

