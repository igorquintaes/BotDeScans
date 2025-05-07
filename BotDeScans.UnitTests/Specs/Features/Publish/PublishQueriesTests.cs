using BotDeScans.App.Features.Publish;
using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;

namespace BotDeScans.UnitTests.Specs.Features.Publish;

public class PublishQueriesTests : UnitQueriesTest, IDisposable
{
    private readonly PublishQueries queries;

    public PublishQueriesTests() =>
        queries = fixture.Create<PublishQueries>();

    public class GetTitleAsync : PublishQueriesTests
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

            var result = await queries.GetTitleAsync(expectedTitle.Id, cancellationToken);

            result.Should().BeEquivalentTo(expectedResult);
        }
    }

    public class GetTitleIdAsync : PublishQueriesTests
    {
        [Fact]
        public async Task GivenExpectedNameShouldReturnTitleId()
        {
            var expectedTitle = fixture.Build<Title>().With(x => x.Id, 1).With(x => x.References, []).Create();
            var unexpectedTitle = fixture.Build<Title>().With(x => x.Id, 2).With(x => x.References, []).Create();

            await fixture.Freeze<DatabaseContext>().AddAsync(expectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(unexpectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().SaveChangesAsync(cancellationToken);

            var result = await queries.GetTitleIdAsync(expectedTitle.Name, cancellationToken);

            result.Should().Be(expectedTitle.Id);
        }
    }
}
