using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.UnitTests.FakeObjects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
namespace BotDeScans.UnitTests.Specs.Features.Publish.Discord;

public class PublishCommandsTests : UnitTest
{
    public class Publish : PublishCommandsTests
    {
        private const int TITLE_ID = 123456;

        private readonly string title;
        private readonly PublishCommands commands;

        public Publish()
        {
            title = fixture.Create<string>();

            fixture.FreezeFake<PublishQueries>();
            fixture.FreezeFake<IFeedbackService>();
            fixture.FreezeFake<IDiscordRestInteractionAPI>();
            fixture.Inject<IOperationContext>(new FakeInteractionContext(fixture));

            commands = fixture.CreateCommand<PublishCommands>(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<PublishQueries>()
                .GetTitleId(title, cancellationToken))
                .Returns(TITLE_ID);

            A.CallTo(() => fixture
                .FreezeFake<IDiscordRestInteractionAPI>()
                .CreateInteractionResponseAsync(
                    ((FakeInteractionContext)fixture.Freeze<IOperationContext>()).Interaction.ID,
                    ((FakeInteractionContext)fixture.Freeze<IOperationContext>()).Interaction.Token,
                    A<InteractionResponse>.Ignored,
                    default,
                    cancellationToken))
                .Returns(new Result());
        }

        [Fact]
        public async Task GivenSuccessExecutionShouldReturnSuccess()
        {
            var result = await commands.Publish(title);

            result.IsSuccess.Should().BeTrue();

            A.CallTo(fixture.FreezeFake<IFeedbackService>()).MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenSuccessExecutionShouldReturnExpectedObjectInMessage()
        {
            await commands.Publish(title);

            var modalData = Fake.GetCalls(fixture
                .FreezeFake<IDiscordRestInteractionAPI>())
                .Select(x => x.Arguments[2].As<InteractionResponse>().Data.Value.Value);

            await Verify(modalData);
        }

        [Fact]
        public async Task GivenNotAnInteractionContextShouldReturnSuccessWithoutExecution()
        {
            fixture.Inject(A.Fake<IOperationContext>());
            var commands = fixture.CreateCommand<PublishCommands>(cancellationToken);

            var result = await commands.Publish(title);

            result.IsSuccess.Should().BeTrue();

            A.CallTo(fixture.FreezeFake<PublishQueries>()).MustNotHaveHappened();
            A.CallTo(fixture.FreezeFake<IFeedbackService>()).MustNotHaveHappened();
            A.CallTo(fixture.FreezeFake<IDiscordRestInteractionAPI>()).MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenNotFoundTitleShouldSendErrorMessage()
        {
            A.CallTo(() => fixture
                .FreezeFake<PublishQueries>()
                .GetTitleId(title, cancellationToken))
                .Returns(null as int?);

            var result = await commands.Publish(title);

            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualErrorAsync(
                    "Obra não encontrada.",
                    default,
                    default,
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorToSendErrorMessageShouldReturnError()
        {
            A.CallTo(() => fixture
                .FreezeFake<PublishQueries>()
                .GetTitleId(title, cancellationToken))
                .Returns(null as int?);


            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualErrorAsync("Obra não encontrada.", default, default, cancellationToken))
                .Returns(new InvalidOperationError());

            var result = await commands.Publish(title);

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task GivenErrorToCreateInteractionShouldReturnError()
        {
            A.CallTo(() => fixture
                .FreezeFake<IDiscordRestInteractionAPI>()
                .CreateInteractionResponseAsync(
                    ((FakeInteractionContext)fixture.Freeze<IOperationContext>()).Interaction.ID,
                    ((FakeInteractionContext)fixture.Freeze<IOperationContext>()).Interaction.Token,
                    A<InteractionResponse>.Ignored,
                    default,
                    cancellationToken))
                .Returns(new InvalidOperationError());

            var result = await commands.Publish(title);

            result.IsSuccess.Should().BeFalse();
        }
    }
}
