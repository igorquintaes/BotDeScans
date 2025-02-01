using AutoFixture;
using AutoFixture.Dsl;
using FakeItEasy;
using System.Linq;
namespace BotDeScans.UnitTests.Extensions;

public static class FixtureExtensions
{
    /// <summary>
    /// Gets or create a freezed fake object
    /// </summary>
    /// <typeparam name="T">type</typeparam>
    /// <param name="fixture">AutoFixture instance</param>
    /// <returns>faked object</returns>
    public static T FreezeFake<T>(this IFixture fixture) where T : class
    {
        if (!fixture.Customizations.Any(x => x is NodeComposer<T>))
            fixture.Inject(A.Fake<T>());

        return fixture.Freeze<T>();
    }
    /// <summary>
    /// Gets or create an array of freezed fake object
    /// </summary>
    /// <typeparam name="T">type</typeparam>
    /// <param name="fixture">AutoFixture instance</param>
    /// <returns>faked object</returns>
    public static T[] FreezeFakes<T>(this IFixture fixture, int quantity) where T : class
    {
        if (!fixture.Customizations.Any(x => x is NodeComposer<T[]>))
        {
            var fakes = Enumerable.Repeat(() => A.Fake<T>(), quantity).Select(x => x.Invoke()).ToArray();
            fixture.Inject(fakes);
        }

        return fixture.Freeze<T[]>();
    }
}
