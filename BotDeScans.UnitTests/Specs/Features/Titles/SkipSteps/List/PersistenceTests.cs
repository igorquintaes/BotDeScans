using BotDeScans.App.Features.Titles.SkipSteps.List;
using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;

namespace BotDeScans.UnitTests.Specs.Features.Titles.SkipSteps.List;

public class PersistenceTests : UnitPersistenceTest
{
    private readonly Persistence persistence;

    public PersistenceTests() =>
        persistence = fixture.Create<Persistence>();

    public class GetStepNamesAsync : PersistenceTests
    {
        [Fact]
        public async Task GivenExpectedIdShouldReturnStepNames()
        {
            var expectedTitle = fixture.Build<Title>().With(x => x.Id, 1).With(x => x.References, []).With(x => x.SkipSteps, []).Create();
            var unexpectedTitle = fixture.Build<Title>().With(x => x.Id, 2).With(x => x.References, []).With(x => x.SkipSteps, []).Create();
            var expectedSkipSteps = fixture.Build<SkipStep>().With(x => x.Id, 1).With(x => x.Title, expectedTitle).Create();
            var expectedSkipSteps2 = fixture.Build<SkipStep>().With(x => x.Id, 2).With(x => x.Step, (StepName)999).With(x => x.Title, expectedTitle).Create();
            var unexpectedSkipSteps = fixture.Build<SkipStep>().With(x => x.Id, 3).With(x => x.Title, unexpectedTitle).Create();
            var expectedResult = new[]
            {
                expectedSkipSteps.Step,
                expectedSkipSteps2.Step
            };
            await fixture.Freeze<DatabaseContext>().AddAsync(expectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(unexpectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(expectedSkipSteps, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(expectedSkipSteps2, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(unexpectedSkipSteps, cancellationToken);
            await fixture.Freeze<DatabaseContext>().SaveChangesAsync(cancellationToken);

            var result = await persistence.GetStepNamesAsync(expectedTitle.Id, cancellationToken);

            result.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task GivenNoneSkipStepsForTitleShouldReturnEmptyList()
        {
            var expectedTitle = fixture.Build<Title>().With(x => x.Id, 1).With(x => x.References, []).With(x => x.SkipSteps, []).Create();
            await fixture.Freeze<DatabaseContext>().AddAsync(expectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().SaveChangesAsync(cancellationToken);

            var result = await persistence.GetStepNamesAsync(expectedTitle.Id, cancellationToken);

            result.Should().BeEmpty();
        }
    }
}
