using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Pings;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction;

public class SetupServiceTests : UnitTest
{
    private readonly SetupService service;

    public SetupServiceTests()
    {
        fixture.FreezeFake<StepsService>();
        fixture.FreezeFake<TitleRepository>();
        fixture.FreezeFake<IValidator<State>>();
        fixture.Inject<IEnumerable<Ping>>([fixture.FreezeFake<Ping>()]);

        service = fixture.Create<SetupService>();
    }

    public class SetupAsync : SetupServiceTests
    {
        private const string PING_TEXT = "some ping text.";

        public SetupAsync()
        {
            var title = fixture.Create<Title>();
            var enabledSteps = fixture.Create<EnabledSteps>();

            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(A<int>._, cancellationToken))
                .Returns(title);

            A.CallTo(() => fixture
                .FreezeFake<StepsService>()
                .GetEnabledSteps(A<IReadOnlyCollection<StepName>>.That
                    .IsSameSequenceAs(title.SkipSteps.Select(s => s.Step))))
                .Returns(enabledSteps);

            A.CallTo(() => fixture
                .FreezeFake<IValidator<State>>()
                .ValidateAsync(A<State>._, cancellationToken))
                .Returns(new ValidationResult());

            A.CallTo(() => fixture
                .FreezeFake<Ping>().IsApplicable)
                .Returns(true);

            A.CallTo(() => fixture
                .FreezeFake<Ping>().GetPingAsTextAsync(
                    A<State>.That.Matches(x =>
                        x.Title == title &&
                        x.Steps == enabledSteps),
                    cancellationToken))
                .Returns(Result.Ok(PING_TEXT));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await service.SetupAsync(fixture.Create<Info>(), cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSetPublishStateChapterInfo()
        {
            var info = fixture.Create<Info>();
            var result = await service.SetupAsync(info, cancellationToken);

            result.Value.ChapterInfo.Should().Be(info);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSetPing()
        {
            var result = await service.SetupAsync(fixture.Create<Info>(), cancellationToken);

            result.Value.PingText.Should().Be(PING_TEXT);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSetPublishStateSteps()
        {
            var info = fixture.Create<Info>();
            var title = fixture
                .Build<Title>()
                .With(x => x.Id, info.TitleId)
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(info.TitleId, cancellationToken))
                .Returns(title);

            var enabledSteps = fixture.Create<EnabledSteps>();
            A.CallTo(() => fixture
                .FreezeFake<StepsService>()
                .GetEnabledSteps(A<IReadOnlyCollection<StepName>>._))
                .Returns(enabledSteps);

            var result = await service.SetupAsync(info, cancellationToken);

            result.Value.Steps.Should().BeSameAs(enabledSteps);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSetPublishStateTitle()
        {
            var info = fixture.Create<Info>();
            var title = fixture
                .Build<Title>()
                .With(x => x.Id, info.TitleId)
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(info.TitleId, cancellationToken))
                .Returns(title);

            var result = await service.SetupAsync(info, cancellationToken);

            result.Value.Title.Should().Be(title);
        }

        [Fact]
        public async Task GivenNullTitleShouldReturnErrorResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(A<int>.Ignored, cancellationToken))
                .Returns(null as Title);

            var result = await service.SetupAsync(fixture.Create<Info>(), cancellationToken);

            result.Should().BeFailure().And.HaveError("Obra não encontrada.");
        }

        [Fact]
        public async Task GivenValidationErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<IValidator<State>>()
                .ValidateAsync(A<State>._, cancellationToken))
                .Returns(new ValidationResult([new ValidationFailure("prop", ERROR_MESSAGE)]));

            var result = await service.SetupAsync(fixture.Create<Info>(), cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenPingErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<Ping>()
                .GetPingAsTextAsync(A<State>._, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.SetupAsync(fixture.Create<Info>(), cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
