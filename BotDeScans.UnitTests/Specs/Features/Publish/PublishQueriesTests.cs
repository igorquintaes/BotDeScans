using BotDeScans.App.Features.Publish;
using BotDeScans.App.Infra;
using BotDeScans.App.Models;

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
            var expectedTitle = fixture.Create<Title>() with { Id = 1, References = [] };
            var unexpectedTitle = fixture.Create<Title>() with { Id = 2, References = [] };
            var expectedReference = fixture.Create<TitleReference>() with { Id = 1, TitleId = expectedTitle.Id, Title = default! };
            var unexpectedReference = fixture.Create<TitleReference>() with { Id = 2, TitleId = unexpectedTitle.Id, Title = default! };
            var expectedResult = expectedTitle with { References = [expectedReference] };

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
            var expectedTitle = fixture.Create<Title>() with { Id = 1, References = [] };
            var unexpectedTitle = fixture.Create<Title>() with { Id = 2, References = [] };

            await fixture.Freeze<DatabaseContext>().AddAsync(expectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().AddAsync(unexpectedTitle, cancellationToken);
            await fixture.Freeze<DatabaseContext>().SaveChangesAsync(cancellationToken);

            var result = await queries.GetTitleIdAsync(expectedTitle.Name, cancellationToken);

            result.Should().Be(expectedTitle.Id);
        }
    }
}
