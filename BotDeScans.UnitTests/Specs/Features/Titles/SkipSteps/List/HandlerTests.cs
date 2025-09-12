using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Titles.SkipSteps.List;
using BotDeScans.App.Models.Entities.Enums;

namespace BotDeScans.UnitTests.Specs.Features.Titles.SkipSteps.List;

public class HandlerTests : UnitTest
{
    private readonly Handler handler;

    public HandlerTests()
    {
        fixture.FreezeFake<Persistence>();

        handler = fixture.Create<Handler>();
    }

    public class ExecuteAsync : HandlerTests
    {
        private readonly int titleId;

        public ExecuteAsync()
        {
            titleId = fixture.Create<int>();

            A.CallTo(() => fixture
                .FreezeFake<Persistence>()
                .GetStepNamesAsync(titleId, cancellationToken))
                .Returns(
                [
                    StepName.UploadMangadex,
                    StepName.UploadSakuraMangas,
                ]);
        }

        [Fact]
        public async Task GivenStepNamesFoundShouldReturnExpectedStringList()
        {
            var result = await handler.ExecuteAsync(titleId, cancellationToken);

            var expectedStrings = new[]
            {
                $"1. {StepName.UploadMangadex.GetDescription()}",
                $"2. {StepName.UploadSakuraMangas.GetDescription()}"
            };

            result.Should().BeEquivalentTo(expectedStrings);
        }

        [Fact]
        public async Task GivenNoReferencesFoundShouldReturnExpectedStringList()
        {
            A.CallTo(() => fixture
                .FreezeFake<Persistence>()
                .GetStepNamesAsync(titleId, cancellationToken))
                .Returns([]);

            var result = await handler.ExecuteAsync(titleId, cancellationToken);

            var expectedStrings = new[] { "A obra não contém procedimentos de publicação a serem ignorados." };

            result.Should().BeEquivalentTo(expectedStrings);
        }
    }
}
