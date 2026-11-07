using BotDeScans.App.Models.Entities;
using BotDeScans.IntegrationTests.Fixtures;
using BotDeScans.IntegrationTests.Specs.Application;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BotDeScans.IntegrationTests.Specs;

[Collection(ApplicationTestCollection.Name)]
public sealed class ApplicationTests(ApplicationTestFixture fixture) : ApplicationTest(fixture)
{
    [Fact]
    public async Task Application_should_start_with_an_in_memory_database()
    {
        Database.Titles.Add(new Title
        {
            Name = "Integration test title",
            DiscordRoleId = 1
        });

        await Database.SaveChangesAsync(TestContext.Current.CancellationToken);

        var title = await Database.Titles.SingleAsync(TestContext.Current.CancellationToken);

        title.Name.Should().Be("Integration test title");
    }
}
