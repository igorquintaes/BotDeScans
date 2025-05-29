using BotDeScans.App.Features.References.Update;
using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;

namespace BotDeScans.UnitTests.Specs.Features.References.Update;

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
            var unexpectedReference = fixture.Build<TitleReference>().With(x => x.Id, 2).With(x => x.Title, unexpectedTitle).Create();
            var expectedResult = new Title
            {
                Id = expectedTitle.Id,
                Name = expectedTitle.Name,
                References = [expectedReference],
                DiscordRoleId = expectedTitle.DiscordRoleId
            };

            await fixture.Freeze<DatabaseContext>().AddAsync(expectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(unexpectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(expectedReference, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(unexpectedReference, cancellationToken);
            await fixture.Freeze<DatabaseContext>().SaveChangesAsync(cancellationToken);

            var result = await persistence.GetTitleAsync(expectedTitle.Name, cancellationToken);

            result.Should().BeEquivalentTo(expectedResult);
        }
    }
}
