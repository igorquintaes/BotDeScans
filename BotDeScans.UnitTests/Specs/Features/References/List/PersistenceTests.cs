using BotDeScans.App.Features.References.List;
using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;

namespace BotDeScans.UnitTests.Specs.Features.References.List;
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
            var expectedReference = fixture.Build<TitleReference>().With(x => x.Id, 1).With(x => x.Title, expectedTitle).Create();
            var expectedReference2 = fixture.Build<TitleReference>().With(x => x.Id, 2).With(x => x.Key, (ExternalReference)999).With(x => x.Title, expectedTitle).Create();
            var unexpectedReference = fixture.Build<TitleReference>().With(x => x.Id, 3).With(x => x.Title, unexpectedTitle).Create();
            var expectedResult = new TitleReference[] { expectedReference, expectedReference2 };

            await fixture.Freeze<DatabaseContext>().AddAsync(expectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(unexpectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(expectedReference, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(expectedReference2, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(unexpectedReference, cancellationToken);
            await fixture.Freeze<DatabaseContext>().SaveChangesAsync(cancellationToken);

            var result = await persistence.GetReferencesAsync(expectedTitle.Name, cancellationToken);

            result.Should().BeEquivalentTo(expectedResult);
        }
    }
}
