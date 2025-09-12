using BotDeScans.App.Features.References.Update;
using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models.Entities;
using FluentValidation;
using FluentValidation.Results;

namespace BotDeScans.UnitTests.Specs.Features.References.Update;

public class HandlerTests : UnitTest
{
    public readonly Handler handler;

    public HandlerTests()
    {
        fixture.FreezeFake<IValidator<Request>>();
        fixture.FreezeFake<TitleRepository>();

        handler = fixture.Create<Handler>();
    }

    public class ExecuteAsync : HandlerTests
    {
        private readonly Title title;
        private readonly Request request;

        public ExecuteAsync()
        {
            title = fixture
                .Build<Title>()
                .With(x => x.References, [])
                .Create();

            request = fixture.Create<Request>();

            A.CallTo(() => fixture
                .FreezeFake<IValidator<Request>>()
                .Validate(request))
                .Returns(new ValidationResult());

            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(request.TitleId, cancellationToken))
                .Returns(title);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await handler.ExecuteAsync(request, cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldUpdateReferences()
        {
            await handler.ExecuteAsync(request, cancellationToken);

            title.References.Where(reference =>
                reference.Key == request.ReferenceKey &&
                reference.Value == request.ReferenceValue)
                .Should().HaveCount(1);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSaveChanges()
        {
            await handler.ExecuteAsync(request, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .SaveAsync(cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenNullTitleShouldReturnErrorResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(request.TitleId, cancellationToken))
                .Returns(null as Title);

            var result = await handler.ExecuteAsync(request, cancellationToken);

            result.Should().BeFailure().And.HaveError("Obra não encontrada.");
        }

        [Fact]
        public async Task GivenValidationErrorShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<IValidator<Request>>()
                .Validate(request))
                .Returns(new ValidationResult([new ValidationFailure("some key", "some error.")]));

            var result = await handler.ExecuteAsync(request, cancellationToken);

            result.Should().BeFailure().And.HaveError("some error.");
        }
    }
}
