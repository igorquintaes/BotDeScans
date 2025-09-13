using BotDeScans.App.Features.Titles.SkipSteps.Add;
using BotDeScans.App.Infra;
using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;

namespace BotDeScans.UnitTests.Specs.Features.Titles.SkipSteps.Add;

public class HandlerTests : UnitTest
{
    private readonly Handler handler;

    public HandlerTests()
    {
        fixture.FreezeFake<TitleRepository>();
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
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(A<int>.Ignored, cancellationToken))
                .Returns(title);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await handler.ExecuteAsync(
                fixture.Create<int>(), 
                fixture.Create<StepName>(), 
                cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldAddData()
        {
            var stepName = fixture.Create<StepName>();

            await handler.ExecuteAsync(fixture.Create<int>(), stepName, cancellationToken);

            title.SkipSteps.Should().ContainSingle()
                 .Which.Step.Should().Be(stepName);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldPersist()
        {
            var titleId = fixture.Create<int>();

            var result = await handler.ExecuteAsync(
                titleId, 
                fixture.Create<StepName>(), 
                cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(titleId, cancellationToken))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<DatabaseContext>()
                .SaveChangesAsync(cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenNullTitleShouldReturnFailResult()
        {
            var titleId = fixture.Create<int>();

            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(titleId, cancellationToken))
                .Returns(null as Title);

            var result = await handler.ExecuteAsync(
                titleId,
                fixture.Create<StepName>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError("Obra não encontrada.");
        }

        [Fact]
        public async Task GivenNullTitleShouldNotPersist()
        {
            var titleId = fixture.Create<int>();

            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(titleId, cancellationToken))
                .Returns(null as Title);

            var result = await handler.ExecuteAsync(
                titleId,
                fixture.Create<StepName>(),
                cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<DatabaseContext>()
                .SaveChangesAsync(cancellationToken))
                .MustNotHaveHappened();
        }
    }
}
