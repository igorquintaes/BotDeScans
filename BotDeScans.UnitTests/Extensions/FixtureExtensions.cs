using AutoFixture;
using AutoFixture.Dsl;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
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

    public static void FreezeFakeConfiguration(this IFixture fixture, string key, string? value)
    {
        if (!fixture.Customizations.Any(x => x is NodeComposer<IConfiguration>))
            fixture.Inject(A.Fake<IConfiguration>());

        var fakeSection = A.Fake<IConfigurationSection>();
        A.CallTo(() => fakeSection.Value).Returns(value);

        A.CallTo(() => fixture
            .FreezeFake<IConfiguration>()
            .GetSection(key))
            .Returns(fakeSection);
    }

    public static void FreezeFakeConfiguration(this IFixture fixture, string key, IEnumerable<string> values)
    {
        if (!fixture.Customizations.Any(x => x is NodeComposer<IConfiguration>))
            fixture.Inject(A.Fake<IConfiguration>());

        var innerFakeSections = values.Select(value =>
        {
            var innerFakeSection = A.Fake<IConfigurationSection>();
            A.CallTo(() => innerFakeSection.Value).Returns(value);

            return innerFakeSection;
        });

        var baseFakeSection = A.Fake<IConfigurationSection>();
        A.CallTo(() => fixture
            .FreezeFake<IConfiguration>()
            .GetSection(key))
            .Returns(baseFakeSection);

        A.CallTo(() => baseFakeSection.GetChildren())
            .Returns(innerFakeSections);
    }
}
