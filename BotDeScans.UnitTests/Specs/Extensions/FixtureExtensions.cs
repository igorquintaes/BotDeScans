using AutoFixture;
using AutoFixture.Dsl;
using FakeItEasy;
using iText.StyledXmlParser.Node;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace BotDeScans.UnitTests.Specs.Extensions;

public static class FixtureExtensions
{
    /// <summary>
    /// Gets or create a freezed fake object
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
    /// <summary>
    /// Gets or create an array of freezed fake object
    /// </summary>
    /// <typeparam name="T">type</typeparam>
    /// <param name="fixture">AutoFixture instance</param>
    /// <returns>faked object</returns>
    public static T[] Fake<T>(this IFixture fixture, int quantity) where T : class
    {
        if (!fixture.Customizations.Any(x => x is NodeComposer<T[]>))
        {
            var fakes = Enumerable.Repeat(() => A.Fake<T>(), quantity).Select(x => x.Invoke()).ToArray();
            fixture.Inject<T[]>(fakes);
        }

        return fixture.Freeze<T[]>();
    }
}
