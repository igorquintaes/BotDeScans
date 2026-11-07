
using Xunit;
using Xunit.v3;
using Xunit.Sdk;

[assembly: Parallelization(Mode = ParallelMode.None)]
namespace BotDeScans.IntegrationTests;

public abstract class IntegrationTest : IAsyncLifetime
{
    public IntegrationTest()
    {
    }

    public async ValueTask InitializeAsync()
    {
    }

    public async ValueTask DisposeAsync()
    {
    }
}
