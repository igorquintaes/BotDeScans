using BotDeScans.App.Features.Titles.SkipSteps.Remove;
using BotDeScans.App.Infra;
using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;
using FluentAssertions.Execution;

namespace BotDeScans.UnitTests.Specs.Features.Titles.SkipSteps.Remove;

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
                .With(x => x.SkipSteps,
                [
                    new SkipStep { Step = StepName.UploadMangadex },
                    new SkipStep { Step = StepName.UploadSakuraMangas },
                ])
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(A<int>.Ignored, cancellationToken))
                .Returns(title);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccess()
        {
            var result = await handler.ExecuteAsync(title.Id, StepName.UploadSakuraMangas, cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldRemoveDataFromEntity()
        {
            await handler.ExecuteAsync(title.Id, StepName.UploadSakuraMangas, cancellationToken);

            title.SkipSteps.Should().ContainSingle()
                 .Which.Step.Should().Be(StepName.UploadMangadex);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldRemoveDataFromDatabase()
        {
            await handler.ExecuteAsync(title.Id, StepName.UploadSakuraMangas, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(title.Id, cancellationToken))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<DatabaseContext>()
                .SaveChangesAsync(cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenNullTitleShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(title.Id, cancellationToken))
                .Returns(null as Title);

            var result = await handler.ExecuteAsync(
                title.Id, 
                fixture.Create<StepName>(), 
                cancellationToken);

            using var _ = new AssertionScope();
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
