using BotDeScans.App.Features.Publish.Interaction.Steps;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class IStepDefaultsTests : UnitTest
{
    [Fact]
    public void ContinueOnErrorShouldDefaultToFalse()
    {
        var step = A.Fake<IStep>(x => x.CallsBaseMethods());
        step.ContinueOnError.Should().BeFalse();
    }
}
