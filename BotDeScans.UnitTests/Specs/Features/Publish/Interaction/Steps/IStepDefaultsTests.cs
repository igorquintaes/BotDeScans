using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class IStepDefaultsTests : UnitTest
{
    [Fact]
    public void ContinueOnErrorShouldDefaultToFalse()
    {
        IStep step = new StepWithoutContinueOnError();
        step.ContinueOnError.Should().BeFalse();
    }

    private sealed class StepWithoutContinueOnError : IStep
    {
        public Task<Result<State>> ExecuteAsync(
            State state,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public StepName Name =>
            throw new NotImplementedException();

        public StepType Type =>
            throw new NotImplementedException();
    }
}
