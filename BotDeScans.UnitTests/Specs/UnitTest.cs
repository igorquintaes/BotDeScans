using AutoFixture.AutoFakeItEasy;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace BotDeScans.UnitTests.Specs;

public abstract class UnitTest
{
    protected readonly IFixture fixture = CreateFixture();
    protected readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    protected static IFixture CreateFixture()
    {
        var fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
        fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        return fixture;
    }
}
