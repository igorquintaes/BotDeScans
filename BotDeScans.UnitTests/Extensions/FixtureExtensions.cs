using AutoFixture.Dsl;
using FakeItEasy.Creation;
using Microsoft.Extensions.Configuration;
using Remora.Commands.Groups;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

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

    public static IConfiguration FreezeFakeConfiguration(this IFixture fixture, string key, string? value)
    {
        var fakeSection = A.Fake<IConfigurationSection>();
        A.CallTo(() => fakeSection.Value).Returns(value);

        A.CallTo(() => fixture
            .FreezeFake<IConfiguration>()
            .GetSection(key))
            .Returns(fakeSection);

        return fixture.FreezeFake<IConfiguration>();
    }

    public static IConfiguration FreezeFakeConfiguration(this IFixture fixture, string key, IEnumerable<string> values)
    {
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

        return fixture.FreezeFake<IConfiguration>();
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

    public static T CreateCustom<T>(
        this IFixture _,
        Dictionary<string, object?> propertyValues)
    {
        var instance = RuntimeHelpers.GetUninitializedObject(typeof(T));

        foreach (var (propertyName, value) in propertyValues)
            SetPropertyValue(instance, typeof(T), propertyName, value);

        return (T)instance;
    }

    public static T CreateCustom<T>(
        this IFixture fixture,
        Action<PropertySetter<T>> propertySetters)
    {
        var setter = new PropertySetter<T>();
        propertySetters(setter);

        return fixture.CreateCustom<T>(setter.PropertyValues);
    }

    private static void SetPropertyValue(object instance, Type type, string propertyName, object? value)
    {
        var field = type.GetField(
            $"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (field is not null)
        {
            field.SetValue(instance, value);
        }
        else
        {
            var property = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (property?.CanWrite == true)
            {
                property.SetValue(instance, value);
            }
            else if (type.BaseType is not null)
            {
                SetPropertyValue(instance, type.BaseType, propertyName, value);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Property '{propertyName}' not found or is not settable on type '{type.Name}'.");
            }
        }
    }
}

public class PropertySetter<T>
{
    internal Dictionary<string, object?> PropertyValues { get; } = [];

    public PropertySetter<T> With<TProperty>(
        Expression<Func<T, TProperty>> propertyExpression,
        TProperty value)
    {
        var propertyName = GetPropertyName(propertyExpression);
        PropertyValues[propertyName] = value;
        return this;
    }

    private static string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
    {
        return propertyExpression.Body switch
        {
            MemberExpression memberExpression => memberExpression.Member.Name,
            UnaryExpression { Operand: MemberExpression memberExpression } => memberExpression.Member.Name,
            _ => throw new ArgumentException(
                "Expression must be a property accessor",
                nameof(propertyExpression))
        };
    }
}
