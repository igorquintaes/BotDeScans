using AutoFixture;
using AutoFixture.Dsl;
using FakeItEasy;
using System.Linq;

namespace BotDeScans.UnitTests.Specs.Extensions;

public static class FixtureExtensions 
{
    /// <summary>
    /// Gets or create a freezed faked object
    /// </summary>
    /// <typeparam name="T">type</typeparam>
    /// <param name="fixture">AutoFixture instance</param>
    /// <returns>faked object</returns>
    public static T Fake<T>(this IFixture fixture) where T : class
    {
        if (!fixture.Customizations.Any(x => x is NodeComposer<T>))
            fixture.Inject<T>(A.Fake<T>());

        return fixture.Freeze<T>();
    }
}
