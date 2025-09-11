using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Services.Discord.Autocomplete;

namespace BotDeScans.UnitTests.Specs.Services.Discord.Autocomplete;

public class AutocompleteStepNamesTests : UnitTest
{
    private readonly AutocompleteStepNames autocomplete;

    public AutocompleteStepNamesTests() =>
        autocomplete = fixture.Create<AutocompleteStepNames>();

    public class Identity : AutocompleteStepNamesTests
    {
        [Fact]
        public void GivenValidIdentityShouldReturnExpectedType() => 
            autocomplete.Identity.Should().Be("autocomplete::stepnames");
    }

    public class GetSuggestionsAsync : AutocompleteStepNamesTests
    {
        [Fact]
        public async Task GivenEmptyRequestShouldReturnAllStepNames()
        {
            var expectedStepNames = Enum
                .GetValues<StepName>()
                .Where(x => (int)x > AutocompleteStepNames.LAST_STEPS_MANAGEMENT_VALUE_IN_ENUM)
                .ToArray();

            var result = await autocomplete.GetSuggestionsAsync(default!, string.Empty, cancellationToken);

            result.Count.Should().Be(expectedStepNames.Length);
            result.Select(s => s.Name).Should().BeEquivalentTo(expectedStepNames.Select(x => x.GetDescription()));
            result.Select(s => s.Value.ToString()).Should().BeEquivalentTo(expectedStepNames.Select(x => $"System.String: {x}"));
        }

        [Fact]
        public async Task GivenMatchingRequestShouldReturnExpectedStepNames()
        {
            var query = "- Mega";
            var expectedStepNames = new StepName[] { StepName.UploadZipMega, StepName.UploadPdfMega };

            var result = await autocomplete.GetSuggestionsAsync(default!, query, cancellationToken);

            result.Count.Should().Be(expectedStepNames.Length);
            result.Select(s => s.Name).Should().BeEquivalentTo(expectedStepNames.Select(x => x.GetDescription()));
            result.Select(s => s.Value.ToString()).Should().BeEquivalentTo(expectedStepNames.Select(x => $"System.String: {x}"));
        }

        [Fact]
        public async Task GivenMatchingInsensitiveRequestShouldReturnExpectedStepNames()
        {
            var query = "- MEGA";
            var expectedStepNames = new StepName[] { StepName.UploadZipMega, StepName.UploadPdfMega };

            var result = await autocomplete.GetSuggestionsAsync(default!, query, cancellationToken);

            result.Count.Should().Be(expectedStepNames.Length);
            result.Select(s => s.Name).Should().BeEquivalentTo(expectedStepNames.Select(x => x.GetDescription()));
            result.Select(s => s.Value.ToString()).Should().BeEquivalentTo(expectedStepNames.Select(x => $"System.String: {x}"));
        }

        [Fact]
        public async Task GivenNoneMatchingRequestShouldReturnEmpty()
        {
            var query = "non-matching-title";
            var result = await autocomplete.GetSuggestionsAsync(default!, query, cancellationToken);

            result.Should().BeEmpty();
        }
    }
}

