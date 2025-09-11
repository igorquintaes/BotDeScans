using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities;
using FluentResults;
using System.ComponentModel;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction;

public class HandlerTests : UnitTest
{
    private readonly Handler handler;

    public HandlerTests()
    {
        fixture.Freeze<State>().Title.SkipSteps.Clear();
        fixture.FreezeFake<DiscordPublisher>();
        handler = fixture.Create<Handler>();
    }

    public class ExecuteAsync : HandlerTests
    {
        public ExecuteAsync()
        {
            fixture.Freeze<State>().Steps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { A.Fake<IManagementStep>(), A.Fake<StepInfo>() },
                { A.Fake<IManagementStep>(), A.Fake<StepInfo>() },
                { A.Fake<IPublishStep>(), A.Fake<StepInfo>() },
                { A.Fake<IPublishStep>(), A.Fake<StepInfo>() },
            });

            A.CallTo(() => fixture.Freeze<State>().Steps.PublishSteps.First().Step.Name)
                .Returns(StepName.UploadZipGoogleDrive);

            A.CallTo(() => fixture.Freeze<State>().Steps.PublishSteps.Last().Step.Name)
                .Returns(StepName.UploadMangadex);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await handler.ExecuteAsync(cancellationToken);
            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallExpectedChainMethods()
        {
            await handler.ExecuteAsync(cancellationToken);

            var managementStep1 = fixture.Freeze<State>().Steps.ManagementSteps.First().Step;
            var managementStep2 = fixture.Freeze<State>().Steps.ManagementSteps.Last().Step;
            var publishStep1 = fixture.Freeze<State>().Steps.PublishSteps.First().Step;
            var publishStep2 = fixture.Freeze<State>().Steps.PublishSteps.Last().Step;

            A.CallTo(() => managementStep1.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly()
                .Then(A.CallTo(() => managementStep2.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => publishStep1.ValidateAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => publishStep2.ValidateAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => publishStep1.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => publishStep2.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallExpectedQuantityOfUpdateTrackingMessage()
        {
            await handler.ExecuteAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .UpdateTrackingMessageAsync(cancellationToken))
                .MustHaveHappened(7, Times.Exactly);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallExpectedQuantityOfUpdateStepStatus()
        {
            await handler.ExecuteAsync(cancellationToken);

            var managementInfo1 = fixture.Freeze<State>().Steps.ManagementSteps.First().Info;
            var managementInfo2 = fixture.Freeze<State>().Steps.ManagementSteps.Last().Info;
            var publishInfo1 = fixture.Freeze<State>().Steps.PublishSteps.First().Info;
            var publishInfo2 = fixture.Freeze<State>().Steps.PublishSteps.Last().Info;

            A.CallTo(() => managementInfo1.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => managementInfo2.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => publishInfo1.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedTwiceExactly();
            A.CallTo(() => publishInfo2.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedTwiceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallExpectedChain()
        {
            await handler.ExecuteAsync(cancellationToken);

            var (managementStep1, managementInfo1) = fixture.Freeze<State>().Steps.ManagementSteps.First();
            var (managementStep2, managementInfo2) = fixture.Freeze<State>().Steps.ManagementSteps.Last();
            var (publishStep1, publishInfo1) = fixture.Freeze<State>().Steps.PublishSteps.First();
            var (publishStep2, publishInfo2) = fixture.Freeze<State>().Steps.PublishSteps.Last();

            A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened()
                .Then(A.CallTo(() => managementStep1.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => managementInfo1.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => managementStep2.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => managementInfo2.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => publishStep1.ValidateAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => publishInfo1.UpdateStatus(A<Result>.Ignored)).MustHaveHappened())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => publishStep2.ValidateAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => publishInfo2.UpdateStatus(A<Result>.Ignored)).MustHaveHappened())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => publishStep1.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => publishInfo1.UpdateStatus(A<Result>.Ignored)).MustHaveHappened())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => publishStep2.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => publishInfo2.UpdateStatus(A<Result>.Ignored)).MustHaveHappened())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened());
        }

        [Fact]
        public async Task GivenErrorExecutionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error message";
            A.CallTo(() => fixture
                .Freeze<State>()
                .Steps.PublishSteps.First().Step
                .ValidateAsync(cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await handler.ExecuteAsync(cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenExceptionExecutionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "Fatal error ocurred. More information inside log file.";
            A.CallTo(() => fixture
                .Freeze<State>()
                .Steps.PublishSteps.First().Step
                .ValidateAsync(cancellationToken))
                .Throws(new Exception("some message."));

            var result = await handler.ExecuteAsync(cancellationToken);
            result.Should().BeFailure().And
                .HaveError(ERROR_MESSAGE).And
                .Match(result =>
                    result.Errors.Count == 1 &&
                    result.Errors.First().Reasons
                        .Select(reason => reason.Message)
                        .Contains("some message."));
        }

        [Fact]
        public async Task GivenErrorExecutionShouldBreakChainCall()
        {
            var (managementStep1, managementInfo1) = fixture.Freeze<State>().Steps.ManagementSteps.First();
            var (managementStep2, managementInfo2) = fixture.Freeze<State>().Steps.ManagementSteps.Last();
            var (publishStep1, publishInfo1) = fixture.Freeze<State>().Steps.PublishSteps.First();
            var (publishStep2, publishInfo2) = fixture.Freeze<State>().Steps.PublishSteps.Last();

            A.CallTo(() => publishStep1
                .ValidateAsync(cancellationToken))
                .Returns(Result.Fail("some error message"));

            await handler.ExecuteAsync(cancellationToken);

            // Call chain
            A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened()
                .Then(A.CallTo(() => managementStep1.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => managementInfo1.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => managementStep2.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => managementInfo2.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => publishStep1.ValidateAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => publishInfo1.UpdateStatus(A<Result>.Ignored)).MustHaveHappened())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened());

            // Not executed due error
            A.CallTo(() => publishStep1.ExecuteAsync(cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep2.ValidateAsync(cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep2.ExecuteAsync(cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishInfo2.UpdateStatus(A<Result>.Ignored)).MustNotHaveHappened();

            // Update tracking error times
            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .UpdateTrackingMessageAsync(cancellationToken))
                .MustHaveHappened(4, Times.Exactly);

            // Publish step was interrupted in validation, so its UpdateStatus must be called once exactly
            A.CallTo(() => publishInfo1
                .UpdateStatus(A<Result>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenExceptionExecutionShouldBreakChainCall()
        {
            var (managementStep1, managementInfo1) = fixture.Freeze<State>().Steps.ManagementSteps.First();
            var (managementStep2, managementInfo2) = fixture.Freeze<State>().Steps.ManagementSteps.Last();
            var (publishStep1, publishInfo1) = fixture.Freeze<State>().Steps.PublishSteps.First();
            var (publishStep2, publishInfo2) = fixture.Freeze<State>().Steps.PublishSteps.Last();

            A.CallTo(() => publishStep1
                .ValidateAsync(cancellationToken))
                .Throws(new Exception("some message."));

            await handler.ExecuteAsync(cancellationToken);

            // Call chain
            A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened()
                .Then(A.CallTo(() => managementStep1.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => managementInfo1.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => managementStep2.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => managementInfo2.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => publishStep1.ValidateAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => publishInfo1.UpdateStatus(A<Result>.Ignored)).MustHaveHappened())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened());

            // Not executed due error
            A.CallTo(() => publishStep1.ExecuteAsync(cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep2.ValidateAsync(cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep2.ExecuteAsync(cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishInfo2.UpdateStatus(A<Result>.Ignored)).MustNotHaveHappened();

            // Update tracking error times
            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .UpdateTrackingMessageAsync(cancellationToken))
                .MustHaveHappened(4, Times.Exactly);

            // Publish step was interrupted in validation, so its UpdateStatus must be called once exactly
            A.CallTo(() => publishInfo1
                .UpdateStatus(A<Result>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorUpdateTrackingMessageShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error message";
            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .UpdateTrackingMessageAsync(cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await handler.ExecuteAsync(cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorUpdateTrackingMessageShouldBreakChainCall()
        {
            var (managementStep1, managementInfo1) = fixture.Freeze<State>().Steps.ManagementSteps.First();
            var (managementStep2, managementInfo2) = fixture.Freeze<State>().Steps.ManagementSteps.Last();
            var (publishStep1, publishInfo1) = fixture.Freeze<State>().Steps.PublishSteps.First();
            var (publishStep2, publishInfo2) = fixture.Freeze<State>().Steps.PublishSteps.Last();

            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .UpdateTrackingMessageAsync(cancellationToken))
                .ReturnsNextFromSequence(Result.Ok(), Result.Ok(), Result.Ok(), Result.Fail("some error message"));

            await handler.ExecuteAsync(cancellationToken);

            // Call chain
            A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened()
                .Then(A.CallTo(() => managementStep1.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => managementInfo1.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => managementStep2.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => managementInfo2.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => publishStep1.ValidateAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => publishInfo1.UpdateStatus(A<Result>.Ignored)).MustHaveHappened())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened());

            // Not executed due error
            A.CallTo(() => publishStep1.ExecuteAsync(cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep2.ValidateAsync(cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep2.ExecuteAsync(cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishInfo2.UpdateStatus(A<Result>.Ignored)).MustNotHaveHappened();

            // Update tracking error times
            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .UpdateTrackingMessageAsync(cancellationToken))
                .MustHaveHappened(4, Times.Exactly);

            // Publish step was interrupted in validation, so its UpdateStatus must be called once exactly
            A.CallTo(() => publishInfo1
                .UpdateStatus(A<Result>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenStepToBeSkippedShouldNotCallItsValidatorNeitherItsPublisher()
        {
            fixture.Freeze<State>().Title.SkipSteps.Add(new SkipStep { Step = fixture.Freeze<State>().Steps.PublishSteps.First().Step.Name });
            await handler.ExecuteAsync(cancellationToken);

            var (managementStep1, managementInfo1) = fixture.Freeze<State>().Steps.ManagementSteps.First();
            var (managementStep2, managementInfo2) = fixture.Freeze<State>().Steps.ManagementSteps.Last();
            var (publishStep1, publishInfo1) = fixture.Freeze<State>().Steps.PublishSteps.First();
            var (publishStep2, publishInfo2) = fixture.Freeze<State>().Steps.PublishSteps.Last();

            var a = fixture.Freeze<State>().Steps.PublishSteps;

            A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened()
                .Then(A.CallTo(() => managementStep1.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => managementInfo1.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => managementStep2.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => managementInfo2.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => publishInfo1.SetToSkip()).MustHaveHappened())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => publishStep2.ValidateAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => publishInfo2.UpdateStatus(A<Result>.Ignored)).MustHaveHappened())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => publishStep2.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => publishInfo2.UpdateStatus(A<Result>.Ignored)).MustHaveHappened())
                .Then(A.CallTo(() => fixture.FreezeFake<DiscordPublisher>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened());


            A.CallTo(() => publishStep1.ValidateAsync(cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep1.ExecuteAsync(cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishInfo1.UpdateStatus(A<Result>.Ignored)).MustNotHaveHappened();
        }

        [Fact]
        [Description("Occurs into SetupStep, when it queries database to find title.")]
        public async Task GivenNullTitleShouldNotSkipStep()
        {
            fixture.Freeze<State>().Title = null!;
            fixture.Freeze<State>().Steps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { A.Fake<IManagementStep>(), A.Fake<StepInfo>() }
            });

            await handler.ExecuteAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .UpdateTrackingMessageAsync(cancellationToken))
                .MustHaveHappened(2, Times.Exactly);

            A.CallTo(() => fixture.Freeze<State>().Steps.ManagementSteps.Single().Step
                .ExecuteAsync(cancellationToken))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture.Freeze<State>().Steps.ManagementSteps.Single().Info
                .UpdateStatus(A<Result>.Ignored))
                .MustHaveHappenedOnceExactly();
        }
    }
}
