using BotDeScans.UnitTests.Helpers;

namespace BotDeScans.UnitTests.Specs;

public abstract class UnitPersistenceTest : UnitTest, IDisposable
{
    protected new readonly IFixture fixture = CreateDatabaseFixture();

    private static IFixture CreateDatabaseFixture()
    {
        var fixture = CreateFixture();
        var contextFactory = new TestDatabaseFactory();
        var context = contextFactory.CreateContext();
        fixture.Inject(contextFactory);
        fixture.Inject(context);

        return fixture;
    }

    public void Dispose()
    {
        fixture.Freeze<TestDatabaseFactory>().Dispose();
        GC.SuppressFinalize(this);
    }
}
