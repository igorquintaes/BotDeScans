using BotDeScans.App.Features.Titles.SkipSteps.Add;
using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;

namespace BotDeScans.UnitTests.Specs.Features.Titles.SkipSteps.Add;

public class HandlerTests : UnitTest
{
    private readonly Handler handler;

    public HandlerTests()
    {
        fixture.FreezeFake<Persistence>();
        fixture.FreezeFake<DatabaseContext>();

        handler = fixture.Create<Handler>();
    }

    public class ExecuteAsync : HandlerTests
    {
        private readonly Title title;

        public ExecuteAsync()
        {
            title = fixture
                .Build<Title>()
                .With(x => x.SkipSteps, [])
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<Persistence>()
                .GetTitleAsync(A<int>.Ignored, cancellationToken))
                .Returns(title);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldAddDataAndPersistIt()
        {
            var titleId = fixture.Create<int>();
            var stepName = fixture.Create<StepName>();

            await handler.ExecuteAsync(titleId, stepName, cancellationToken);

            title.SkipSteps.Should().ContainSingle()
                 .Which.Step.Should().Be(stepName);

            A.CallTo(() => fixture
                .FreezeFake<Persistence>()
                .GetTitleAsync(titleId, cancellationToken))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<DatabaseContext>()
                .SaveChangesAsync(cancellationToken))
                .MustHaveHappenedOnceExactly();
        }
    }
}
