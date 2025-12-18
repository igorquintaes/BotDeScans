using BotDeScans.App.Extensions;
using BotDeScans.App.Models.Entities.Enums;
using Microsoft.Extensions.Configuration;
using static FluentAssertions.FluentActions;

namespace BotDeScans.UnitTests.Specs.Extensions;

public abstract class ConfigurationExtensionsTests : UnitTest
{
    public class GetRequiredValue : ConfigurationExtensionsTests
    {
        [Fact]
        public void GivenMissingKeyShouldThrowArgumentNullException()
        {
            const string KEY = "MissingKey";
            const string ERROR = $"'{KEY}' config value not found. (Parameter 'key')";

            Invoking(() => fixture
                   .FreezeFakeConfiguration(KEY, null as string)
                   .GetRequiredValue<string>(KEY))
                   .Should().Throw<ArgumentNullException>()
                   .WithMessage(ERROR);
        }

        [Fact]
        public void GivenExistingKeyShouldReturnValue()
        {
            const string KEY = "ExistingKey";
            const string VALUE = "ExpectedValue";

            fixture.FreezeFakeConfiguration(KEY, VALUE)
                   .GetRequiredValue<string>(KEY)
                   .Should().Be(VALUE);
        }
    }

    public class GetRequiredValueAsResult : ConfigurationExtensionsTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void GivenNotFilledValShouldReturnFailResult(string? value)
        {
            const string KEY = "SomeKey";
            const string ERROR = $"'{KEY}' config value not found.";

            fixture.FreezeFakeConfiguration(KEY, value)
                   .GetRequiredValueAsResult<string>(KEY)
                   .Should().BeFailure()
                   .And.HaveError(ERROR);
        }

        [Fact]
        public void GivenInvalidValueForATypeShouldReturnFailResult()
        {
            const string KEY = "SomeKey";
            const string ERROR = $"'{KEY}' config value contains an unsupported value.";

            fixture.FreezeFakeConfiguration(KEY, "invalid-value")
                   .GetRequiredValueAsResult<int>(KEY)
                   .Should().BeFailure()
                   .And.HaveError(ERROR);
        }

        [Fact]
        public void GivenValidValueShouldConvertAndReturnIt()
        {
            const string KEY = "SomeKey";
            const string VALUE = nameof(StepName.Setup);

            fixture.FreezeFakeConfiguration(KEY, VALUE)
                   .GetRequiredValueAsResult<StepName>(KEY)
                   .Should().BeSuccess()
                   .And.HaveValue(StepName.Setup);
        }
    }

    public class GetValues : ConfigurationExtensionsTests
    {
        [Fact]
        public void GivenNotFoundSectionShouldReturnEmpty()
        {
            const string SECTION = "SomeSection";

            A.CallTo<IConfigurationSection?>(() => fixture
                   .FreezeFake<IConfiguration>()
                   .GetSection(SECTION))
                   .Returns(null);

            var result = fixture
                   .FreezeFake<IConfiguration>()
                   .GetValues<string>(SECTION)
                   .Should().BeEmpty();
        }

        [Fact]
        public void GivenEmptyListShouldReturnEmpty()
        {
            const string SECTION = "SomeSection";
            
            fixture.FreezeFakeConfiguration(SECTION, [])
                   .GetValues<string>(SECTION)
                   .Should().BeEmpty();
        }

        [Fact]
        public void GivenValidValuesShouldReturnParsedDistinctValues()
        {
            const string SECTION = "SomeSection";
            var values = new string[]
            {
                nameof(StepName.Download),
                nameof(StepName.Compress)
            };

            var expectedResult = new StepName[]
            {
                StepName.Download,
                StepName.Compress
            };

            fixture.FreezeFakeConfiguration(SECTION, values)
                   .GetValues<StepName>(SECTION)
                   .Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void GivenRepeatedValuesShouldIgnoreThem()
        {
            const string SECTION = "SomeSection";
            var values = new string[]
            {
                nameof(StepName.Download),
                nameof(StepName.Download),
                nameof(StepName.Download),
                nameof(StepName.Compress),
                nameof(StepName.Compress)
            };

            var expectedResult = new StepName[]
            {
                StepName.Download,
                StepName.Compress
            };

            fixture.FreezeFakeConfiguration(SECTION, values)
                   .GetValues<StepName>(SECTION)
                   .Should().BeEquivalentTo(expectedResult);
        }
    }
}
