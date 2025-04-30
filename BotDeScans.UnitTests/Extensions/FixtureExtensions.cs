using AutoFixture.Dsl;
using FakeItEasy.Creation;
using Microsoft.Extensions.Configuration;
using Remora.Commands.Groups;
using System.Reflection;
namespace BotDeScans.UnitTests.Extensions;

public static class FixtureExtensions
{
    /// <summary>
    /// Creates a string with expected length
    /// </summary>
    /// <typeparam name="T">type</typeparam>
    /// <param name="fixture">AutoFixture instance</param>
    /// <param name="length">expected length</param>
    /// <returns>string with expected length</returns>
    public static string StringOfLength(this IFixture fixture, int length)
        => string.Join("", fixture.CreateMany<char>(length));

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
    /// Gets or create a freezed fake object
    /// </summary>
    /// <typeparam name="T">type</typeparam>
    /// <param name="fixture">AutoFixture instance</param>
    /// <param name="optionsBuilder">FakeItEasy options to create fake object</param>
    /// <returns>faked object</returns>
    public static T FreezeFake<T>(this IFixture fixture, Action<IFakeOptions<T>> optionsBuilder) where T : class
    {
        if (!fixture.Customizations.Any(x => x is NodeComposer<T>))
            fixture.Inject(A.Fake<T>(optionsBuilder));

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
        fixture.FreezeFake<IConfiguration>();

        var fakeSection = A.Fake<IConfigurationSection>();
        A.CallTo(() => fakeSection.Value).Returns(value);

        A.CallTo(() => fixture
            .FreezeFake<IConfiguration>()
            .GetSection(key))
            .Returns(fakeSection);
    }

    public static void FreezeFakeConfiguration(this IFixture fixture, string key, IEnumerable<string> values)
    {
        fixture.FreezeFake<IConfiguration>();

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

    public static T CreateCommand<T>(this IFixture fixture, CancellationToken cancellationToken)
        where T : CommandGroup
    {
        var command = fixture.Create<T>();

        command.GetType()
            .GetMethod("SetCancellationToken", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(command, [cancellationToken]);

        return command;
    }
}
