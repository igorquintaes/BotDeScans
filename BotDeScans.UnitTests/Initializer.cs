using System.Runtime.CompilerServices;
namespace BotDeScans.UnitTests;

internal static class Initializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        DerivePathInfo((sourceFile, projectDirectory, type, method) => new(
             directory: Path.Combine(projectDirectory, "Snapshots"),
             typeName: ($"{type.Namespace!.Replace("BotDeScans.UnitTests.Specs", "")}.{type.Name}").TrimStart('.'),
             methodName: method.Name));

        VerifyFakeItEasy.Initialize();
    }
}
