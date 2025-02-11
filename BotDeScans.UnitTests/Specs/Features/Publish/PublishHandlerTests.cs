using AutoFixture;
using BotDeScans.App.Features.Publish;
using BotDeScans.UnitTests.Extensions;
using FakeItEasy;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BotDeScans.UnitTests.Specs.Features.Publish;

public class PublishHandlerTests : UnitTest
{
    private readonly PublishHandler handler;

    public PublishHandlerTests()
    {
        fixture.FreezeFake<PublishService>();

        handler = fixture.Create<PublishHandler>();
    }

    public class HandleAsync : PublishHandlerTests
    {
        private readonly string pingContent;
        public HandleAsync()
        {
            pingContent = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<PublishService>()
                .CreatePingMessageAsync(cancellationToken))
                .Returns(Result.Ok(pingContent));
        }

        [Fact]
        public async Task GivenSuccessFulExecutionShouldReturnSuccessResult()
        {
            var result = await handler.HandleAsync(() => Task.FromResult(Result.Ok()), cancellationToken);

            result.Should().BeSuccess().And.HaveValue(pingContent);
        }

        [Fact]
        public async Task GivenErrorToCreatePingMessageShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<PublishService>()
                .CreatePingMessageAsync(cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await handler.HandleAsync(() => Task.FromResult(Result.Ok()), cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToValidateBeforeFilesManagementAsyncShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<PublishService>()
                .ValidateBeforeFilesManagementAsync(cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await handler.HandleAsync(() => Task.FromResult(Result.Ok()), cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToRuFeedbackFuncShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";
            static Task<Result> func() => Task.FromResult(Result.Fail(ERROR_MESSAGE));

            var result = await handler.HandleAsync(func, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToRunManagementStepsAsyncShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";
            Func<Task<Result>> func = () => Task.FromResult(Result.Ok());

            A.CallTo(() => fixture
                .FreezeFake<PublishService>()
                .RunManagementStepsAsync(func, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await handler.HandleAsync(func, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToValidateAfterFilesManagementAsyncShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<PublishService>()
                .ValidateAfterFilesManagementAsync(cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await handler.HandleAsync(() => Task.FromResult(Result.Ok()), cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToRunPublishStepsAsyncShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";
            Func<Task<Result>> func = () => Task.FromResult(Result.Ok());

            A.CallTo(() => fixture
                .FreezeFake<PublishService>()
                .RunPublishStepsAsync(func, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await handler.HandleAsync(func, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
