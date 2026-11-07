
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.IntegrationTests.Fixtures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BotDeScans.IntegrationTests.Specs.Database.Infra;

public class DatabaseContextTests(DatabaseTestFixture databaseTestFixture)
    : DatabaseTest(databaseTestFixture)
{
    public class Uniques(DatabaseTestFixture databaseTestFixture)
        : DatabaseContextTests(databaseTestFixture)
    {
        [Fact]
        public async Task Should_Have_Unique_Constraint_On_Title_Name()
        {
            var titleA = CreateTitle(1);
            var titleB = CreateTitle(1);

            Context.Titles.Add(titleA);
            Context.Titles.Add(titleB);

            Func<Task> act = async () => await Context.SaveChangesAsync(CancellationToken);

            var exception = await act.Should()
                .ThrowExactlyAsync<DbUpdateException>();


            exception
                .WithInnerExceptionExactly<SqliteException>()
                .WithMessage("SQLite Error 19: 'UNIQUE constraint failed: Titles.Name'.");
        }

        [Fact]
        public async Task Should_Have_Unique_Constraint_On_TitleReference_Key()
        {
            var title = CreateTitle(1);
            var referenceA = CreateTitleReference(title, 1);
            var referenceB = CreateTitleReference(title, 1);

            Context.Titles.Add(title);
            Context.TitleReferences.Add(referenceA);
            Context.TitleReferences.Add(referenceB);

            Func<Task> act = async () => await Context.SaveChangesAsync(CancellationToken);

            var exception = await act.Should()
                .ThrowExactlyAsync<DbUpdateException>();

            exception
                .WithInnerExceptionExactly<SqliteException>()
                .WithMessage("SQLite Error 19: 'UNIQUE constraint failed: TitleReferences.TitleId, TitleReferences.Key'.");
        }

        [Fact]
        public async Task Should_Have_Unique_Constraint_On_SkipStep_Step()
        {
            var title = CreateTitle(1);
            var skipStepA = CreateSkipStep(title, 1);
            var skipStepB = CreateSkipStep(title, 1);

            Context.Titles.Add(title);
            Context.SkipSteps.Add(skipStepA);
            Context.SkipSteps.Add(skipStepB);

            Func<Task> act = async () => await Context.SaveChangesAsync(CancellationToken);

            var exception = await act.Should()
                .ThrowExactlyAsync<DbUpdateException>();

            exception
                .WithInnerExceptionExactly<SqliteException>()
                .WithMessage("SQLite Error 19: 'UNIQUE constraint failed: SkipSteps.TitleId, SkipSteps.Step'.");
        }
    }

    public class Relationships(DatabaseTestFixture databaseTestFixture)
        : DatabaseContextTests(databaseTestFixture)
    {
        [Fact]
        public async Task Should_Delete_Title_And_Related_Entities_When_Title_Is_Deleted()
        {
            var title = CreateTitle(1);
            var reference = CreateTitleReference(title, 1);
            var skipStep = CreateSkipStep(title, 1);

            Context.Titles.Add(title);
            Context.TitleReferences.Add(reference);
            Context.SkipSteps.Add(skipStep);

            await Context.SaveChangesAsync(CancellationToken);

            Context.Titles.Remove(title);
            await Context.SaveChangesAsync(CancellationToken);

            var titlesCount = await Context.Titles.CountAsync(CancellationToken);
            var referencesCount = await Context.TitleReferences.CountAsync(CancellationToken);
            var skipStepsCount = await Context.SkipSteps.CountAsync(CancellationToken);

            titlesCount.Should().Be(0);
            referencesCount.Should().Be(0);
            skipStepsCount.Should().Be(0);
        }
    }

    private static Title CreateTitle(int index) =>
        new()
        {
            Name = $"Integration test title {index}",
            DiscordRoleId = (ulong?)index
        };

    private static TitleReference CreateTitleReference(Title title, int index) =>
        new()
        {
            Title = title,
            Key =  (ExternalReference)index,
            Value = $"value_{index}"
        };

    private static SkipStep CreateSkipStep(Title title, int index) =>
        new()
        {
            Title = title,
            Step = (StepName)index
        };
}
