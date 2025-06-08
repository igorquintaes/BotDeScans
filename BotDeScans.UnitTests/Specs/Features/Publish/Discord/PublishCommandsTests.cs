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
    public class PublishAsync : PublishCommandsTests
    {
        private const int titleId = 123456;
        private readonly PublishCommands commands;

        public PublishAsync()
        {
            fixture.FreezeFake<PublishQueries>();
            fixture.FreezeFake<IDiscordRestInteractionAPI>();
            fixture.Inject<IOperationContext>(new FakeInteractionContext(fixture));

            commands = fixture.CreateCommand<PublishCommands>(cancellationToken);

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
            var result = await commands.PublishAsync(titleId);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task GivenSuccessExecutionShouldReturnExpectedObjectInMessage()
        {
            await commands.PublishAsync(titleId);

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

            var result = await commands.PublishAsync(titleId);

            result.IsSuccess.Should().BeTrue();

            A.CallTo(fixture.FreezeFake<PublishQueries>()).MustNotHaveHappened();
            A.CallTo(fixture.FreezeFake<IFeedbackService>()).MustNotHaveHappened();
            A.CallTo(fixture.FreezeFake<IDiscordRestInteractionAPI>()).MustNotHaveHappened();
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

            var result = await commands.PublishAsync(titleId);

            result.IsSuccess.Should().BeFalse();
        }
    }
}
