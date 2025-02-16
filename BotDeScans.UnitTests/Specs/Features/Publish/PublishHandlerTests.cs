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
using iText.Pdfua.Checkers.Utils;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.Commands.Contexts;
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
        fixture.FreezeFake<PublishQueries>();
        fixture.FreezeFake<IValidator<Info>>();

        handler = fixture.Create<PublishHandler>();
    }

    public class HandleAsync : PublishHandlerTests
    {
        private readonly string pingContent;
        public HandleAsync()
        {
            pingContent = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<IValidator<Info>>()
                .Validate(fixture.Freeze<Info>()))
                .Returns(new ValidationResult());

            A.CallTo(() => fixture
                .FreezeFake<PublishQueries>()
                .GetTitle(fixture.Freeze<Info>().TitleId, cancellationToken))
                .Returns(fixture.Freeze<Title>());

            A.CallTo(() => fixture
                .FreezeFake<PublishService>()
                .CreatePingMessageAsync(cancellationToken))
                .Returns(Result.Ok(pingContent));
        }

        [Fact]
        public async Task GivenSuccessFulExecutionShouldReturnSuccessResult()
        {
            var result = await handler.HandleAsync(
                fixture.Freeze<Info>(),
                fixture.Freeze<InteractionContext>(),
                cancellationToken);

            result.Should().BeSuccess().And.HaveValue(pingContent);
        }

        [Fact]
        public async Task GivenErrorToValidateInfoDataShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<IValidator<Info>>()
                .Validate(fixture.Freeze<Info>()))
                .Returns(new ValidationResult([new ValidationFailure { ErrorMessage = ERROR_MESSAGE }]));

            var result = await handler.HandleAsync(
                fixture.Freeze<Info>(),
                fixture.Freeze<InteractionContext>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenNoneTitleFoundInDatabaseShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "Obra não encontrada.";

            A.CallTo(() => fixture
                .FreezeFake<PublishQueries>()
                .GetTitle(fixture.Freeze<Info>().TitleId, cancellationToken))
                .Returns(null as Title);

            var result = await handler.HandleAsync(
                fixture.Freeze<Info>(),
                fixture.Freeze<InteractionContext>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToCreateTrackingMessageShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<PublishMessageService>()
                .SendOrEditTrackingMessageAsync(
                    fixture.Freeze<InteractionContext>(),
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await handler.HandleAsync(
                fixture.Freeze<Info>(),
                fixture.Freeze<InteractionContext>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToCreatePingMessageShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<PublishService>()
                .CreatePingMessageAsync(cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await handler.HandleAsync(
                fixture.Freeze<Info>(),
                fixture.Freeze<InteractionContext>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToValidateBeforeFilesManagementAsyncShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<PublishService>()
                .ValidateBeforeFilesManagementAsync(
                    fixture.Freeze<InteractionContext>(), 
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await handler.HandleAsync(
                fixture.Freeze<Info>(),
                fixture.Freeze<InteractionContext>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToRunManagementStepsAsyncShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<PublishService>()
                .RunManagementStepsAsync(
                    fixture.Freeze<InteractionContext>(), 
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await handler.HandleAsync(
                fixture.Freeze<Info>(),
                fixture.Freeze<InteractionContext>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToValidateAfterFilesManagementAsyncShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<PublishService>()
                .ValidateAfterFilesManagementAsync(
                    fixture.Freeze<InteractionContext>(), 
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await handler.HandleAsync(
                fixture.Freeze<Info>(),
                fixture.Freeze<InteractionContext>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToRunPublishStepsAsyncShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<PublishService>()
                .RunPublishStepsAsync(
                    fixture.Freeze<InteractionContext>(), 
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await handler.HandleAsync(
                fixture.Freeze<Info>(),
                fixture.Freeze<InteractionContext>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
