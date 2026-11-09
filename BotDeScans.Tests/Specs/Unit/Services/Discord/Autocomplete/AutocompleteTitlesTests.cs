using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services.Discord.Autocomplete;
using FluentAssertions.Execution;

namespace BotDeScans.Tests.Specs.Unit.Services.Discord.Autocomplete;

public class AutocompleteTitlesTests : UnitTest
{
    private readonly AutocompleteTitles autocomplete;

    public AutocompleteTitlesTests()
    {
        fixture.FreezeFake<TitleRepository>();
        autocomplete = fixture.Create<AutocompleteTitles>();
    }

    public class Identity : AutocompleteTitlesTests
    {
        [Fact]
        public void GivenValidIdentityShouldReturnExpectedType() =>
            autocomplete.Identity.Should().Be("autocomplete::titles");
    }

    public class GetSuggestionsAsync : AutocompleteTitlesTests
    {
        private readonly IReadOnlyList<Title> databaseTitles;

        public GetSuggestionsAsync()
        {
            databaseTitles = [.. Enumerable
                .Range(1, 30)
                .Select(i => new Title
                {
                    Id = i,
                    Name = $"title-{i.ToString().PadLeft(2, '0')}",
                    DiscordRoleId = (ulong)i
                })];

            A.CallTo( () =>fixture.Freeze<TitleRepository>()
                   .GetTitlesByNameLikeAsync(
                        nameLike: A<string>.Ignored,
                        resultsQuantity: Consts.DISCORD_CHOICE_MAX_RESULTS,
                        cancellationToken))
                   .Returns(databaseTitles);
        }

        [Fact]
        public async Task GivenMaxTitleLenghtShouldReturnItWithoutEscaping()
        {
            var maxLenghtTitle = "c".PadRight(Consts.DISCORD_PARAM_MAX_LENGTH, 'a');
            var titleId = 50;


            var databaseTitles = new List<Title>
            {
                new()
                {
                    Id = titleId,
                    Name = maxLenghtTitle,
                    DiscordRoleId = default
                }
            };

            A.CallTo(() => fixture.Freeze<TitleRepository>()
                   .GetTitlesByNameLikeAsync(
                        nameLike: A<string>.Ignored,
                        resultsQuantity: Consts.DISCORD_CHOICE_MAX_RESULTS,
                        cancellationToken))
                   .Returns(databaseTitles);

            var result = await autocomplete.GetSuggestionsAsync(default!, maxLenghtTitle, cancellationToken);

            using var _ = new AssertionScope();
            result.Count.Should().Be(1);
            result.Select(s => s.Name).Should().BeEquivalentTo([maxLenghtTitle]);
            result.Select(s => s.Value.ToString()).Should().BeEquivalentTo([$"System.Int32: {titleId}"]);
        }

        [Fact]
        public async Task GivenAboveMaxLimitTitleLenghtShouldReturnItEscaping()
        {
            var aboveMaxTitleLenght = "c".PadRight(Consts.DISCORD_PARAM_MAX_LENGTH + 1, 'a');
            var expectedTitleReturn = "c".PadRight(Consts.DISCORD_PARAM_MAX_LENGTH - 3, 'a') + "...";
            var titleId = 50;

            var databaseTitles = new List<Title>
            {
                new()
                {
                    Id = titleId,
                    Name = aboveMaxTitleLenght,
                    DiscordRoleId = default
                }
            };

            A.CallTo(() => fixture.Freeze<TitleRepository>()
                   .GetTitlesByNameLikeAsync(
                        nameLike: A<string>.Ignored,
                        resultsQuantity: Consts.DISCORD_CHOICE_MAX_RESULTS,
                        cancellationToken))
                   .Returns(databaseTitles);

            var result = await autocomplete.GetSuggestionsAsync(default!, aboveMaxTitleLenght, cancellationToken);

            using var _ = new AssertionScope();
            result.Count.Should().Be(1);
            result.Select(s => s.Name).Should().BeEquivalentTo([expectedTitleReturn]);
            result.Select(s => s.Value.ToString()).Should().BeEquivalentTo([$"System.Int32: {titleId}"]);
        }
    }
}
