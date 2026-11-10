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
        private readonly IReadOnlyList<Title> titles;

        public GetSuggestionsAsync()
        {
            titles = [.. Enumerable
                .Range(1, 30)
                .Select(i => new Title
                {
                    Id = i,
                    Name = $"title-{i.ToString().PadLeft(2, '0')}",
                    DiscordRoleId = (ulong)i
                })];

            A.CallTo(() => fixture.Freeze<TitleRepository>()
                   .GetTitlesByNameLikeAsync(
                        nameLike: A<string>.Ignored,
                        resultsQuantity: Consts.DISCORD_CHOICE_MAX_RESULTS,
                        cancellationToken))
                   .Returns(titles);
        }

        [Fact]
        public async Task GivenMaxTitleLenghtShouldReturnItWithoutEscaping()
        {
            var maxLenghtTitle = "c".PadRight(Consts.DISCORD_PARAM_MAX_LENGTH, 'a');
            var titleId = 50;


            var titles = new List<Title>
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
                        nameLike: maxLenghtTitle,
                        resultsQuantity: Consts.DISCORD_CHOICE_MAX_RESULTS,
                        cancellationToken))
                   .Returns(titles);

            var result = await autocomplete.GetSuggestionsAsync(default!, maxLenghtTitle, cancellationToken);

            using var _ = new AssertionScope();
            result.Count.Should().Be(1);
            result.Select(s => s.Name).Should().BeEquivalentTo([maxLenghtTitle]);
            result.Select(s => s.Value.ToString()).Should().BeEquivalentTo([$"System.Int32: {titleId}"]);


        }

        [Fact]
        public async Task GivenAboveMaxLimitTitleLenghtShouldReturnItEscaping()
        {
            const int TITLE_ID = 50;
            const string TITLE_NAME_REQUEST = "some name";

            var titleNameResult = "c".PadRight(Consts.DISCORD_PARAM_MAX_LENGTH + 1, 'a');
            var titleNameResultTruncated = "c".PadRight(Consts.DISCORD_PARAM_MAX_LENGTH - 3, 'a') + "...";

            var titles = new List<Title>
            {
                new()
                {
                    Id = TITLE_ID,
                    Name = titleNameResult,
                    DiscordRoleId = default
                }
            };

            A.CallTo(() => fixture.Freeze<TitleRepository>()
                   .GetTitlesByNameLikeAsync(
                        nameLike: TITLE_NAME_REQUEST,
                        resultsQuantity: Consts.DISCORD_CHOICE_MAX_RESULTS,
                        cancellationToken))
                   .Returns(titles);

            var result = await autocomplete.GetSuggestionsAsync(default!, TITLE_NAME_REQUEST, cancellationToken);

            using var _ = new AssertionScope();
            result.Count.Should().Be(1);
            result.Select(s => s.Name).Should().BeEquivalentTo([titleNameResultTruncated]);
            result.Select(s => s.Value.ToString()).Should().BeEquivalentTo([$"System.Int32: {TITLE_ID}"]);
        }
    }
}
