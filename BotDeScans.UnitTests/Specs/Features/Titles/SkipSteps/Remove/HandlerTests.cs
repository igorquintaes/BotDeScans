using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Features.Titles.SkipSteps.Remove;
using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;

namespace BotDeScans.UnitTests.Specs.Features.Titles.SkipSteps.Remove;

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
                .With(x => x.SkipSteps,
                [
                    new SkipStep { Step = StepName.UploadMangadex },
                    new SkipStep { Step = StepName.UploadSakuraMangas },
                ])
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<Persistence>()
                .GetTitleAsync(A<int>.Ignored, cancellationToken))
                .Returns(title);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldRemoveData()
        {
            await handler.ExecuteAsync(title.Id, StepName.UploadSakuraMangas, cancellationToken);

            title.SkipSteps.Should().ContainSingle()
                 .Which.Step.Should().Be(StepName.UploadMangadex);

            A.CallTo(() => fixture
                .FreezeFake<Persistence>()
                .GetTitleAsync(title.Id, cancellationToken))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<DatabaseContext>()
                .SaveChangesAsync(cancellationToken))
                .MustHaveHappenedOnceExactly();
        }
    }
}
