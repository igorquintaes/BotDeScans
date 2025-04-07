using System.Runtime.CompilerServices;
namespace BotDeScans.UnitTests;

static class Initializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        UseProjectRelativeDirectory("Snapshots");
        VerifyFakeItEasy.Initialize();
    }
}
