using BotDeScans.App.Infra;
using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models.Entities;
using FluentAssertions.Execution;

namespace BotDeScans.UnitTests.Specs.Infra.Repositories;

public class TitleRepositoryTests : UnitPersistenceTest
{
    private readonly TitleRepository repository;

    public TitleRepositoryTests() =>
        repository = fixture.Create<TitleRepository>();

    public class GetTitleAsync : TitleRepositoryTests
    {
        [Fact]
        public async Task GivenExpectedIdShouldReturnTitle()
        {
            var expectedTitle = fixture.Build<Title>().With(x => x.Id, 1).With(x => x.References, []).With(x => x.SkipSteps, []).Create();
            var unexpectedTitle = fixture.Build<Title>().With(x => x.Id, 2).With(x => x.References, []).With(x => x.SkipSteps, []).Create();
            var expectedSkipSteps = fixture.Build<SkipStep>().With(x => x.Id, 1).With(x => x.Title, expectedTitle).Create();
            var unexpectedSkipSteps = fixture.Build<SkipStep>().With(x => x.Id, 2).With(x => x.Title, unexpectedTitle).Create();
            var expectedReference = fixture.Build<TitleReference>().With(x => x.Id, 1).With(x => x.Title, expectedTitle).Create();
            var unexpectedReference = fixture.Build<TitleReference>().With(x => x.Id, 2).With(x => x.Title, unexpectedTitle).Create();

            await fixture.Freeze<DatabaseContext>().AddAsync(expectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(unexpectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(expectedReference, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(unexpectedReference, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(expectedSkipSteps, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(unexpectedSkipSteps, cancellationToken);
            await fixture.Freeze<DatabaseContext>().SaveChangesAsync(cancellationToken);

            var result = await repository.GetTitleAsync(expectedTitle.Id, cancellationToken);

            using var _ = new AssertionScope();
            result.Should().NotBeNull();
            result?.Should().BeEquivalentTo(expectedTitle);
            result?.SkipSteps.Should().BeEquivalentTo([expectedSkipSteps]);
            result?.References.Should().BeEquivalentTo([expectedReference]);
        }
    }

    public class GetTitlesAsync : TitleRepositoryTests
    {
        [Fact]
        public async Task GivenTitlesInDatabaseShouldReturnAllTitles()
        {
            var expectedTitles = Enumerable.Range(1, 5)
                .Select(i => fixture.Build<Title>()
                    .With(x => x.Id, i)
                    .With(x => x.References, [])
                    .With(x => x.SkipSteps, [])
                    .Create())
                .ToList();

            await fixture.Freeze<DatabaseContext>().AddRangeAsync(expectedTitles, cancellationToken);
            await fixture.Freeze<DatabaseContext>().SaveChangesAsync(cancellationToken);

            var result = await repository.GetTitlesAsync(cancellationToken);

            result.Should().HaveCount(expectedTitles.Count).And
                           .BeEquivalentTo(expectedTitles);
        }
    }
}
