using BotDeScans.App.Features.Titles.Update;
using BotDeScans.App.Models.Entities;
using BotDeScans.UnitTests.FakeObjects;
using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace BotDeScans.UnitTests.Specs.Features.Titles.Update;

public class CommandsTests : UnitTest
{
    private readonly Commands commands;

    public CommandsTests()
    {
        fixture.FreezeFake<Persistence>();
        fixture.FreezeFake<IFeedbackService>();
        fixture.FreezeFake<IDiscordRestInteractionAPI>();
        fixture.Inject<IOperationContext>(new FakeInteractionContext(fixture));

        commands = fixture.CreateCommand<Commands>(cancellationToken);
    }

    public class ExecuteAsync : CommandsTests
    {
        private readonly Title title;

        public ExecuteAsync()
        {
            title = fixture
                .Build<Title>()
                .With(x => x.Id, 3)
                .With(x => x.Name, "test-name")
                .With(x => x.DiscordRoleId, 100UL)
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<Persistence>()
                .GetTitleAsync(title.Id, cancellationToken))
                .Returns(title);

            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(new Remora.Results.Result<IMessage>());
        }

        [Fact]
        public async Task GivenNotAnInteractionContextShouldReturnSuccessWithoutExecution()
        {
            fixture.Inject(A.Fake<IOperationContext>());
            var commands = fixture.CreateCommand<Commands>(cancellationToken);

            var result = await commands.ExecuteAsync(title.Id);

            result.IsSuccess.Should().BeTrue();

            A.CallTo(fixture.FreezeFake<IFeedbackService>()).MustNotHaveHappened();
            A.CallTo(fixture.FreezeFake<IDiscordRestInteractionAPI>()).MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenValidRequestShouldReturnSuccess()
        {
            var result = await commands.ExecuteAsync(title.Id);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task GivenValidRequestShouldReturnExpectedInteraction()
        {
            await commands.ExecuteAsync(title.Id);

            var modalData = Fake.GetCalls(fixture
                .FreezeFake<IDiscordRestInteractionAPI>())
                .Select(x => x.Arguments[2].As<InteractionResponse>().Data.Value.Value);

            await Verify(modalData);
        }

        [Fact]
        public async Task GivenNullTitleErrorShouldReturnSuccess()
        {
            A.CallTo(() => fixture
                .FreezeFake<Persistence>()
                .GetTitleAsync(title.Id, cancellationToken))
                .Returns(null as Title);

            var result = await commands.ExecuteAsync(title.Id);
            result.IsSuccess.Should().BeTrue();

            A.CallTo(() => fixture
                .FreezeFake<Persistence>()
                .GetTitleAsync(title.Id, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenHandlerErrorShouldReturnExpectedEmbed()
        {
            A.CallTo(() => fixture
                .FreezeFake<Persistence>()
                .GetTitleAsync(title.Id, cancellationToken))
                .Returns(null as Title);

            await commands.ExecuteAsync(title.Id);

            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualWarningAsync("Obra não encontrada.", null, null, cancellationToken))
                .MustHaveHappenedOnceExactly();
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

            var result = await commands.ExecuteAsync(title.Id);

            result.IsSuccess.Should().BeFalse();
        }
    }
}
