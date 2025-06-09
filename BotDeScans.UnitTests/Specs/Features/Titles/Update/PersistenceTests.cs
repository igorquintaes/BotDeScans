using BotDeScans.App.Features.Titles.Update;
using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;

namespace BotDeScans.UnitTests.Specs.Features.Titles.Update;

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
            var expectedTitle = fixture.Build<Title>().With(x => x.Id, 1).With(x => x.References, []).Create();
            var unexpectedTitle = fixture.Build<Title>().With(x => x.Id, 2).With(x => x.References, []).Create();

            await fixture.Freeze<DatabaseContext>().AddAsync(expectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(unexpectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().SaveChangesAsync(cancellationToken);

            var result = await persistence.GetTitleAsync(expectedTitle.Id, cancellationToken);

            result.Should().BeEquivalentTo(expectedTitle);
        }
    }
}
