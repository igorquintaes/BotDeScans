using System.Runtime.CompilerServices;
using VerifyTests;
using VerifyXunit;

namespace BotDeScans.UnitTests;

static class Initializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        Verifier.UseProjectRelativeDirectory("Snapshots");
        VerifyFakeItEasy.Initialize();
    }
}
