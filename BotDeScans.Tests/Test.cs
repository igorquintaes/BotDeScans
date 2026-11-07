using System.Runtime.CompilerServices;
using AutoFixture.AutoFakeItEasy;
using Xunit.Sdk;

[assembly: Parallelization(Mode = ParallelMode.None)]
namespace BotDeScans.Tests;

public abstract class Test
{
    protected readonly IFixture fixture = CreateFixture();
    protected readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [ModuleInitializer]
    internal static void Initialize()
    {
        DerivePathInfo((sourceFile, projectDirectory, type, method) => new(
             directory: Path.Combine(projectDirectory, "Snapshots"),
             typeName: $"{type.Namespace!.Replace("BotDeScans.Tests.Specs.", "")}{type.Name}",
             methodName: method.Name));

        VerifyFakeItEasy.Initialize();
        VerifierSettings.UseUtf8NoBom();
    }

    protected static IFixture CreateFixture()
    {
        var fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
        fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        return fixture;
    }
}
