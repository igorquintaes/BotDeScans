using AutoFixture;
using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Infra;
using BotDeScans.App.Models;
using BotDeScans.UnitTests.Extensions;
using FakeItEasy;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Remora.Discord.Commands.Contexts;
using System;
using System.Threading.Tasks;
using Xunit;
using static BotDeScans.App.Features.Publish.PublishState;

namespace BotDeScans.UnitTests.Specs.Features.Publish;

public class PublishHandlerTests : UnitTest
{
    private readonly PublishHandler handler;

    public PublishHandlerTests()
    {
        fixture.FreezeFake<PublishState>();
        fixture.FreezeFake<PublishService>();
        fixture.FreezeFake<PublishMessageService>();
        fixture.FreezeFake<DatabaseContext>();
        fixture.FreezeFake<IValidator<Info>>();

        handler = fixture.Create<PublishHandler>();
    }

    public class HandleAsync : PublishHandlerTests
    {
        private readonly string pingContent;
        public HandleAsync()
        {
            pingContent = fixture.Create<string>();

            fixture.Freeze<Info>();
            fixture.Freeze<Title>();
            fixture.Freeze<InteractionContext>();

            A.CallTo(() => fixture
                .FreezeFake<IValidator<Info>>()
                .Validate(fixture.Freeze<Info>()))
                .Returns(new ValidationResult());

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
