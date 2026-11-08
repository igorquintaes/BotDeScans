using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.Tests.Fixtures;

namespace BotDeScans.Tests.Specs.Database.Infra.Repositories;

public class TitleRepositoryTests(DatabaseTestFixture databaseTestFixture)
    : DatabaseTest(databaseTestFixture)
{
    private readonly TitleRepository repository = new(databaseTestFixture.Context);

    public class GetTitleAsync(DatabaseTestFixture databaseTestFixture)
        : TitleRepositoryTests(databaseTestFixture)
    {
        [Fact]
        public async Task Should_Return_Title_When_Title_Exists()
        {
            var title = CreateTitle(1);
            var skipStepA = CreateSkipStep(title, 1);
            var skipStepB = CreateSkipStep(title, 2);
            var referenceA = CreateTitleReference(title, 1);
            var referenceB = CreateTitleReference(title, 2);

            Context.Titles.Add(title);
            Context.SkipSteps.AddRange([skipStepA, skipStepB]);
            Context.TitleReferences.AddRange([referenceA, referenceB]);

            await Context.SaveChangesAsync(cancellationToken);
            var result = await repository.GetTitleAsync(title.Id, cancellationToken);

            var expectedResult = new Title
            {
                Id = title.Id,
                Name = title.Name,
                DiscordRoleId = title.DiscordRoleId,
                References =
                [
                    referenceA,
                    referenceB
                ],
                SkipSteps =
                [

                    skipStepA,
                    skipStepB
                ]
            };

            result.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task Should_Return_Null_When_Title_Does_Not_Exist()
        {
            var result = await repository.GetTitleAsync(1, cancellationToken);
            result.Should().BeNull();
        }

        [Fact]
        public async Task Should_Return_Title_With_Empty_References_And_SkipSteps_When_No_References_Or_SkipSteps_Exist()
        {
            var title = CreateTitle(1);
            Context.Titles.Add(title);

            await Context.SaveChangesAsync(cancellationToken);

            var result = await repository.GetTitleAsync(title.Id, cancellationToken);

            var expectedResult = new Title
            {
                Id = title.Id,
                Name = title.Name,
                DiscordRoleId = title.DiscordRoleId,
                References = [],
                SkipSteps = []
            };

            result.Should().BeEquivalentTo(expectedResult);
        }
    }

    public class GetTitlesAsync(DatabaseTestFixture databaseTestFixture)
        : TitleRepositoryTests(databaseTestFixture)
    {
        [Fact]
        public async Task Should_Return_All_Titles_When_Titles_Exist()
        {
            var titleA = CreateTitle(1);
            var skipStepA_1 = CreateSkipStep(titleA, 1);
            var skipStepA_2 = CreateSkipStep(titleA, 2);
            var referenceA_1 = CreateTitleReference(titleA, 1);
            var referenceA_2 = CreateTitleReference(titleA, 2);

            Context.Titles.Add(titleA);
            Context.SkipSteps.AddRange([skipStepA_1, skipStepA_2]);
            Context.TitleReferences.AddRange([referenceA_1, referenceA_2]);

            var titleB = CreateTitle(2);
            var skipStepB_1 = CreateSkipStep(titleB, 3);
            var skipStepB_2 = CreateSkipStep(titleB, 4);
            var referenceB_1 = CreateTitleReference(titleB, 3);
            var referenceB_2 = CreateTitleReference(titleB, 4);

            Context.Titles.Add(titleB);
            Context.SkipSteps.AddRange([skipStepB_1, skipStepB_2]);
            Context.TitleReferences.AddRange([referenceB_1, referenceB_2]);

            await Context.SaveChangesAsync(cancellationToken);

            var result = await repository.GetTitlesAsync(cancellationToken);

            var expectedResult = new List<Title>
            {
                new()
                {
                    Id = titleA.Id,
                    Name = titleA.Name,
                    DiscordRoleId = titleA.DiscordRoleId,
                    References = [ referenceA_1, referenceA_2 ],
                    SkipSteps = [ skipStepA_1, skipStepA_2 ]
                },
                new()
                {
                    Id = titleB.Id,
                    Name = titleB.Name,
                    DiscordRoleId = titleB.DiscordRoleId,
                    References = [ referenceB_1, referenceB_2 ],
                    SkipSteps = [ skipStepB_1, skipStepB_2 ]
                }
            };

            result.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task Should_Return_Empty_List_When_No_Titles_Exist()
        {
            var result = await repository.GetTitlesAsync(cancellationToken);
            result.Should().BeEmpty();
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
            Key = (ExternalReference)index,
            Value = $"value_{index}"
        };

    private static SkipStep CreateSkipStep(Title title, int index) =>
        new()
        {
            Title = title,
            Step = (StepName)index
        };
}
