using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction;

public class DiscordPublisherTests : UnitTest, IDisposable
{
    private readonly DiscordPublisher publisher;
    private readonly State state;
    private readonly string coverFilePath;
    private readonly InteractionContext interactionContext;
    private readonly ulong interactionChannelId;

    public DiscordPublisherTests()
    {
        var interactionToken = fixture.Create<string>();
        interactionChannelId = fixture.Create<ulong>();
        interactionContext = CreateInteractionContext(interactionToken, interactionChannelId);

        fixture.Inject<IOperationContext>(interactionContext);
        fixture.FreezeFake<IFeedbackService>();
        fixture.FreezeFake<IDiscordRestInteractionAPI>();
        fixture.FreezeFake<IDiscordRestChannelAPI>();
        fixture.FreezeFake<TextReplacer>();

        fixture.FreezeFakeConfiguration("Discord:ReleaseChannel", "12345");

        coverFilePath = Path.GetTempFileName();
        File.WriteAllText(coverFilePath, "cover");

        var title = fixture.Build<Title>()
            .With(x => x.Name, "My Title")
            .Create();

        var chapterInfo = new Info(
            googleDriveUrl: "https://drive.google.com/drive/folders/1q2w3e4r5t6y7u8i9o",
            chapterName: "Chapter Name",
            chapterNumber: "10",
            chapterVolume: "1",
            message: "Mensagem",
            titleId: 1);

        var state = new State
        {
            Title = title,
            ChapterInfo = chapterInfo,
            ReleaseLinks = new Links { MegaZip = "https://mega.nz/sample" },
            InternalData = new InternalData { CoverFilePath = coverFilePath, Pings = "@everyone" }
        };

        var fakeStep = A.Fake<IManagementStep>();
        A.CallTo(() => fakeStep.Name).Returns(StepName.Download);
        state = state with
        {
            Steps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { fakeStep, new StepInfo(fakeStep) }
            })
        };

        fixture.Inject(state);
        this.state = state;
        publisher = fixture.Create<DiscordPublisher>();
    }

    public void Dispose()
    {
        if (File.Exists(coverFilePath))
            File.Delete(coverFilePath);

        GC.SuppressFinalize(this);
    }

    public class UpdateTrackingMessageAsync : DiscordPublisherTests
    {
        [Fact]
        public async Task GivenFirstTrackingUpdateShouldSendContextualEmbed()
        {
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(Remora.Results.Result<IMessage>.FromSuccess(A.Fake<IMessage>()));

            var result = await publisher.UpdateTrackingMessageAsync(state, cancellationToken);

            result.Should().BeSuccess();
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSecondTrackingUpdateShouldEditFollowupMessage()
        {
            var author = A.Fake<IUser>();
            var authorId = new Snowflake(fixture.Create<ulong>());
            A.CallTo(() => author.ID).Returns(authorId);

            var trackedMessage = A.Fake<IMessage>();
            A.CallTo(() => trackedMessage.Author).Returns(author);
            A.CallTo(() => trackedMessage.ID).Returns(new Snowflake(fixture.Create<ulong>()));

            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(Remora.Results.Result<IMessage>.FromSuccess(trackedMessage));

            A.CallTo(fixture.FreezeFake<IDiscordRestInteractionAPI>())
                .WithReturnType<Task<Remora.Results.Result<IMessage>>>()
                .Returns(Remora.Results.Result<IMessage>.FromSuccess(trackedMessage));

            var firstResult = await publisher.UpdateTrackingMessageAsync(state, cancellationToken);
            await publisher.UpdateTrackingMessageAsync(firstResult.Value, cancellationToken);

            Fake.GetCalls(fixture.FreezeFake<IDiscordRestInteractionAPI>())
                .Count(call => call.Method.Name == nameof(IDiscordRestInteractionAPI.EditFollowupMessageAsync))
                .Should().Be(1);
        }

        [Fact]
        public async Task GivenTrackingSendErrorShouldReturnFailure()
        {
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(Remora.Results.Result<IMessage>.FromError(new Remora.Results.InvalidOperationError("send failed")));

            var result = await publisher.UpdateTrackingMessageAsync(state, cancellationToken);

            result.Should().BeFailure()
                .And.HaveError("Error to update Discord message.")
                .And.HaveError("send failed");
        }
    }

    public class ErrorReleaseMessageAsync : DiscordPublisherTests
    {
        [Fact]
        public async Task GivenErrorResultShouldSendErrorEmbedToInteractionChannel()
        {
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendEmbedAsync(
                    A<Snowflake>.Ignored,
                    A<Embed>.Ignored,
                    A<FeedbackMessageOptions>.Ignored,
                    cancellationToken))
                .Returns(Remora.Results.Result<IMessage>.FromSuccess(A.Fake<IMessage>()));

            await publisher.ErrorReleaseMessageAsync(FluentResults.Result.Fail("error"), cancellationToken);

            var sendEmbedCall = Fake.GetCalls(fixture.FreezeFake<IFeedbackService>())
                .Single(call => call.Method.Name == nameof(IFeedbackService.SendEmbedAsync));

            ((Snowflake)sendEmbedCall.Arguments[0]!).Value.Should().Be(interactionChannelId);
        }
    }

    public class SuccessReleaseMessageAsync : DiscordPublisherTests
    {
        [Fact]
        public async Task GivenValidStateShouldCreateReleaseMessage()
        {
            var publishState = fixture.Create<State>();

            A.CallTo(() => fixture
                .FreezeFake<TextReplacer>()
                .Replace(A<string>.Ignored, A<State>.Ignored))
                .Returns(fixture.Create<string>());

            await publisher.SuccessReleaseMessageAsync(state, cancellationToken);

            Fake.GetCalls(fixture.FreezeFake<IDiscordRestChannelAPI>())
                .Count(call => call.Method.Name == nameof(IDiscordRestChannelAPI.CreateMessageAsync))
                .Should().Be(1);

            A.CallTo(() => fixture
                .FreezeFake<TextReplacer>()
                .Replace(A<string>.Ignored, A<State>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenEmptyMessageShouldNotCallTextReplacer()
        {
            var publishState = state with
            {
                ChapterInfo = new Info(
                    googleDriveUrl: "https://drive.google.com/drive/folders/1q2w3e4r5t6y7u8i9o",
                    chapterName: "Chapter Name",
                    chapterNumber: "10",
                    chapterVolume: "1",
                    message: "",
                    titleId: 1)
            };

            await publisher.SuccessReleaseMessageAsync(publishState, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<TextReplacer>()
                .Replace(A<string>.Ignored, A<State>.Ignored))
                .MustNotHaveHappened();
        }
    }

    private InteractionContext CreateInteractionContext(string token, ulong channelId)
    {
        var user = fixture.FreezeFake<IUser>();
        A.CallTo(() => user.ID).Returns(new Snowflake(fixture.Create<ulong>()));
        A.CallTo(() => user.Username).Returns(fixture.Create<string>());
        A.CallTo(() => user.Avatar).Returns(A.Fake<IImageHash>());

        var member = fixture.FreezeFake<IGuildMember>();
        A.CallTo(() => member.User).Returns(new Optional<IUser>(user));

        var channel = A.Fake<IPartialChannel>();
        A.CallTo(() => channel.ID).Returns(new Optional<Snowflake>(new Snowflake(channelId)));

        return new InteractionContext(new Remora.Discord.API.Objects.Interaction(
            ID: fixture.Create<Snowflake>(),
            ApplicationID: fixture.Create<Snowflake>(),
            Type: InteractionType.ApplicationCommand,
            Data: default,
            GuildID: fixture.Create<Snowflake>(),
            Channel: new Optional<IPartialChannel>(channel),
            ChannelID: default,
            Member: new Optional<IGuildMember>(member),
            User: default,
            Token: token,
            Version: 1,
            Message: default,
            AppPermissions: default!,
            Locale: default,
            GuildLocale: default,
            Entitlements: default!,
            Context: default,
            AuthorizingIntegrationOwners: default));
    }
}
