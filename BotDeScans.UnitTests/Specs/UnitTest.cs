using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using Bogus;
using System.Threading;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace BotDeScans.UnitTests.Specs;

public abstract class UnitTest
{
    protected readonly IFixture fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
    protected readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;
    protected readonly Faker dataGenerator = new();
}
