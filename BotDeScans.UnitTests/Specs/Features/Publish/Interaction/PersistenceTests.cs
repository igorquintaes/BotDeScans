using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction;

public class PersistenceTests : UnitPersistenceTest, IDisposable
{
    private readonly Persistence queries;

    public PersistenceTests() =>
        queries = fixture.Create<Persistence>();

    public class GetTitleAsync : PersistenceTests
    {
        [Fact]
        public async Task GivenExpectedIdShouldReturnTitle()
        {
            var expectedTitle = fixture.Build<Title>().With(x => x.Id, 1).With(x => x.References, []).With(x => x.SkipSteps, []).Create();
            var unexpectedTitle = fixture.Build<Title>().With(x => x.Id, 2).With(x => x.References, []).With(x => x.SkipSteps, []).Create();
            var expectedReference = fixture.Build<TitleReference>().With(x => x.Id, 1).With(x => x.Title, expectedTitle).Create();
            var unexpectedReference = fixture.Build<TitleReference>().With(x => x.Id, 2).With(x => x.Title, unexpectedTitle).Create();
            var expectedSkipSteps = fixture.Build<SkipStep>().With(x => x.Id, 1).With(x => x.Title, expectedTitle).Create();
            var unexpectedSkipSteps = fixture.Build<SkipStep>().With(x => x.Id, 2).With(x => x.Title, unexpectedTitle).Create();
            var expectedResult = new Title
            {
                Id = expectedTitle.Id,
                Name = expectedTitle.Name,
                References = [expectedReference],
                SkipSteps = [expectedSkipSteps],
                DiscordRoleId = expectedTitle.DiscordRoleId
            };

            await fixture.Freeze<DatabaseContext>().AddAsync(expectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(unexpectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(expectedReference, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(unexpectedReference, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(expectedSkipSteps, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(unexpectedSkipSteps, cancellationToken);
            await fixture.Freeze<DatabaseContext>().SaveChangesAsync(cancellationToken);

            var result = await queries.GetTitleAsync(expectedTitle.Id, cancellationToken);

            result.Should().BeEquivalentTo(expectedResult);
        }
    }
}
