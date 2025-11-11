using BotDeScans.App.Infra;
using BotDeScans.App.Models;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services.Discord.Autocomplete;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace BotDeScans.UnitTests.Specs.Services.Discord.Autocomplete;

public class AutocompleteTitlesTests : UnitPersistenceTest
{
    private readonly AutocompleteTitles autocomplete;

    public AutocompleteTitlesTests() => 
        autocomplete = fixture.Create<AutocompleteTitles>();

    public class Identity : AutocompleteTitlesTests
    {
        [Fact]
        public void GivenValidIdentityShouldReturnExpectedType() => 
            autocomplete.Identity.Should().Be("autocomplete::titles");
    }

    public class GetSuggestionsAsync : AutocompleteTitlesTests
    {
        private readonly (string Name, int Id)[] databaseTitles;

        public GetSuggestionsAsync()
        {
            databaseTitles = Enumerable.Range(1, 30)
                .Select(i => ($"title-{i.ToString().PadLeft(2, '0')}", i))
                .ToArray();

            fixture.Freeze<DatabaseContext>()
                   .Titles.AddRange(databaseTitles.Select(databaseTitle => 
                       new Title
                       {
                           Id = databaseTitle.Id,
                           Name = databaseTitle.Name,
                           DiscordRoleId = default
                       }));

            fixture.Freeze<DatabaseContext>().SaveChanges();
        }

        [Fact]
        public async Task GivenEmptyRequestShouldReturnFirst25Titles()
        {
            var expectedNames = databaseTitles.Take(25).Select(x => x.Name);
            var expectedIds = databaseTitles.Take(25).Select(x => $"System.Int32: {x.Id}");

            var result = await autocomplete.GetSuggestionsAsync(default!, string.Empty, cancellationToken);

            result.Count.Should().Be(AutocompleteTitles.DISCORD_MAX_RESULTS);
            result.Select(s => s.Name).Should().BeEquivalentTo(expectedNames);
            result.Select(s => s.Value.ToString()).Should().BeEquivalentTo(expectedIds);
        }

        [Theory]
        [InlineData("title-1")]
        [InlineData("TITLE-1")]
        public async Task GivenMatchingRequestShouldReturnExpectedTitles(string query)
        {
            var expectedNames = databaseTitles
                .Where(x => x.Id >= 10 && x.Id <= 19)
                .Select(x => x.Name);
            var expectedIds = databaseTitles
                .Where(x => x.Id >= 10 && x.Id <= 19)
                .Select(x => $"System.Int32: {x.Id}");

            var result = await autocomplete.GetSuggestionsAsync(default!, query, cancellationToken);

            result.Count.Should().Be(10);
            result.Select(s => s.Name).Should().BeEquivalentTo(expectedNames);
            result.Select(s => s.Value.ToString()).Should().BeEquivalentTo(expectedIds);
        }

        [Fact]
        public async Task GivenNoneMatchingRequestShouldReturnEmpty()
        {
            var query = "non-matching-title";
            var result = await autocomplete.GetSuggestionsAsync(default!, query, cancellationToken);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GivenMaxTitleLenghtShouldReturnItWithoutEscaping()
        {
            var maxLenghtTitle = "c".PadRight(Consts.DISCORD_PARAM_MAX_LENGTH, 'a');
            var maxLenghtTitleId = 999;

            fixture.Freeze<DatabaseContext>().Titles.Add(new()
            {
                Id = maxLenghtTitleId,
                Name = maxLenghtTitle,
                DiscordRoleId = default
            });

            fixture.Freeze<DatabaseContext>().SaveChanges();

            var result = await autocomplete.GetSuggestionsAsync(default!, maxLenghtTitle, cancellationToken);

            using var _ = new AssertionScope();
            result.Count.Should().Be(1);
            result.Select(s => s.Name).Should().BeEquivalentTo([maxLenghtTitle]);
            result.Select(s => s.Value.ToString()).Should().BeEquivalentTo([$"System.Int32: {maxLenghtTitleId}"]);
        }

        [Fact]
        public async Task GivenAboveMaxLimitTitleLenghtShouldReturnItEscaping()
        {
            var aboveMaxTitleLenght = "c".PadRight(Consts.DISCORD_PARAM_MAX_LENGTH+1, 'a');
            var aboveMaxTitleId = 999;
            var expectedTitleReturn = "c".PadRight(Consts.DISCORD_PARAM_MAX_LENGTH-3, 'a') + "...";

            fixture.Freeze<DatabaseContext>().Titles.Add(new()
            {
                Id = aboveMaxTitleId,
                Name = aboveMaxTitleLenght,
                DiscordRoleId = default
            });

            fixture.Freeze<DatabaseContext>().SaveChanges();

            var result = await autocomplete.GetSuggestionsAsync(default!, aboveMaxTitleLenght, cancellationToken);

            using var _ = new AssertionScope();
            result.Count.Should().Be(1);
            result.Select(s => s.Name).Should().BeEquivalentTo([expectedTitleReturn]);
            result.Select(s => s.Value.ToString()).Should().BeEquivalentTo([$"System.Int32: {aboveMaxTitleId}"]);
        }

        [Fact]
        public async Task GivenDataShouldReturnOrderedResult()
        {
            var newTitle = new Title
            {
                Id = 999,
                Name = "000" + databaseTitles[0].Name,
                DiscordRoleId = default
            };

            await fixture.Freeze<DatabaseContext>().Titles.AddAsync(newTitle, cancellationToken);
            fixture.Freeze<DatabaseContext>().SaveChanges();

            var result = await autocomplete.GetSuggestionsAsync(default!, string.Empty, cancellationToken);

            using var _ = new AssertionScope();
            result[0].Name.Should().Be(newTitle.Name);
            result[1].Name.Should().Be(databaseTitles[0].Name);
        }
    }
}
