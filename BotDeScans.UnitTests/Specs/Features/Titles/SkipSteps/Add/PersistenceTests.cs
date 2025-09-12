using BotDeScans.App.Features.Titles.SkipSteps.Add;
using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;
using FluentAssertions.Execution;

namespace BotDeScans.UnitTests.Specs.Features.Titles.SkipSteps.Add;

public class PersistenceTests : UnitPersistenceTest
{
    private readonly Persistence persistence;

    public PersistenceTests() =>
        persistence = fixture.Create<Persistence>();

    public class GetTitleAsync : PersistenceTests
    {
        [Fact]
        public async Task GivenExpectedIdShouldReturnTitle()
        {
            var expectedTitle = fixture.Build<Title>().With(x => x.Id, 1).With(x => x.SkipSteps, []).With(x => x.References, []).Create();
            var unexpectedTitle = fixture.Build<Title>().With(x => x.Id, 2).With(x => x.SkipSteps, []).With(x => x.References, []).Create();
            var expectedSkipSteps = fixture.Build<SkipStep>().With(x => x.Id, 1).With(x => x.Title, expectedTitle).Create();
            var expectedSkipSteps2 = fixture.Build<SkipStep>().With(x => x.Id, 2).With(x => x.Step, (StepName)999).With(x => x.Title, expectedTitle).Create();
            var unexpectedSkipSteps = fixture.Build<SkipStep>().With(x => x.Id, 3).With(x => x.Title, unexpectedTitle).Create();

            await fixture.Freeze<DatabaseContext>().AddAsync(expectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(unexpectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(expectedSkipSteps, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(expectedSkipSteps2, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(unexpectedSkipSteps, cancellationToken);
            await fixture.Freeze<DatabaseContext>().SaveChangesAsync(cancellationToken);

            var result = await persistence.GetTitleAsync(expectedTitle.Id, cancellationToken);

            using var _ = new AssertionScope();
            result.Should().BeEquivalentTo(expectedTitle);
            result.SkipSteps.Should().Contain(expectedSkipSteps);
            result.SkipSteps.Should().Contain(expectedSkipSteps2);
            result.SkipSteps.Should().NotContain(unexpectedSkipSteps);
        }
    }
}
