using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.Pings;
using BotDeScans.App.Features.Publish.State;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Models.Entities;
using FluentValidation;
using FluentValidation.Results;
namespace BotDeScans.UnitTests.Specs.Features.Publish.Steps;

public class SetupStepTests : UnitTest
{
    private readonly SetupStep step;

    public SetupStepTests()
    {
        fixture.Freeze<PublishState>();
        fixture.FreezeFake<PublishQueries>();
        fixture.FreezeFake<IValidator<PublishState>>();
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
                .FreezeFake<IValidator<PublishState>>()
                .ValidateAsync(fixture.Freeze<PublishState>(), cancellationToken))
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
                .With(x => x.Id, fixture.Freeze<PublishState>().ChapterInfo.TitleId)
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<PublishQueries>()
                .GetTitleAsync(title.Id, cancellationToken))
                .Returns(title);

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<PublishState>().Title.Should().Be(title);
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

            fixture.Freeze<PublishState>().InternalData.Pings.Should().Be(ping);
        }

        [Fact]
        public async Task GivenValidationErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<IValidator<PublishState>>()
                .ValidateAsync(fixture.Freeze<PublishState>(), cancellationToken))
                .Returns(new ValidationResult([new ValidationFailure("prop", ERROR_MESSAGE)]));

            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}

