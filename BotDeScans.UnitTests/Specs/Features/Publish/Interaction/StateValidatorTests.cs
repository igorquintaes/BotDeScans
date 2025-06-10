using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Pings;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services.Discord;
using FluentResults;
using FluentValidation;
using FluentValidation.TestHelper;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction;

public class StateValidatorTests : UnitTest
{
    private readonly State data;

    public StateValidatorTests()
    {
        fixture.FreezeFake<RolesService>();
        fixture.FreezeFake<IValidator<Info>>();
        fixture.FreezeFake<IValidator<Title>>();

        fixture.FreezeFakeConfiguration(GlobalPing.GLOBAL_ROLE_KEY, "some-role");
        fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, PingType.Global.ToString());

        var titleReferente = new TitleReference { Key = ExternalReference.MangaDex, Value = fixture.Create<string>(), Title = default! };

        data = fixture
            .Build<State>()
            .With(x => x.Title, fixture
                .Build<Title>()
                .With(x => x.References, [titleReferente])
                .Create())
            .With(x => x.Steps, new EnabledSteps(new Dictionary<IStep, StepInfo>
                { { A.Fake<UploadMangaDexStep>(), A.Fake<StepInfo>() } }))
            .Create();
    }

    [Fact]
    public async Task GivenSuccessfulDataShouldReturnValid()
    {
        var result = await fixture
              .Create<StateValidator>()
              .TestValidateAsync(data, default, cancellationToken);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task GivenSuccessfulDataShouldCallInnerValidators()
    {
        await fixture
              .Create<StateValidator>()
              .TestValidateAsync(data, default, cancellationToken);

        A.CallTo(() => fixture
              .FreezeFake<IValidator<Title>>()
              .ValidateAsync(
                  A<IValidationContext>.That.Matches(x => (Title)x.InstanceToValidate == data.Title),
                  cancellationToken))
              .MustHaveHappenedOnceExactly();

        A.CallTo(() => fixture
              .FreezeFake<IValidator<Info>>()
              .ValidateAsync(
                  A<IValidationContext>.That.Matches(x => (Info)x.InstanceToValidate == data.ChapterInfo),
                  cancellationToken))
              .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GivenSuccessfulDataWithoutMangaDexReferenceShouldReturnValid()
    {
        var data = fixture
              .Build<State>()
              .With(x => x.Title, fixture
                  .Build<Title>()
                  .With(x => x.References, [])
                  .Create())
              .With(x => x.Steps, new EnabledSteps(new Dictionary<IStep, StepInfo>
                  { { A.Fake<IStep>(), A.Fake<StepInfo>() } }))
              .Create();

        var result = await fixture
              .Create<StateValidator>()
              .TestValidateAsync(data, default, cancellationToken);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task GivenMangadexStepShouldReturnInvalidWhenReferenceIsNotSet()
    {
        var data = fixture
              .Build<State>()
              .With(x => x.Title, fixture
                  .Build<Title>()
                  .With(x => x.References, [])
                  .Create())
              .With(x => x.Steps, new EnabledSteps(new Dictionary<IStep, StepInfo>
                  { { A.Fake<UploadMangaDexStep>(), A.Fake<StepInfo>() } }))
              .Create();

        var result = await fixture
              .Create<StateValidator>()
              .TestValidateAsync(data, default, cancellationToken);

        result.ShouldHaveValidationErrorFor(prop => prop.Title)
              .WithErrorMessage("Não foi definida uma referência para a publicação da obra na MangaDex.");
    }

    [Fact]
    public async Task GivenSakuraMangasStepShouldReturnInvalidWhenReferenceIsNotSet()
    {
        var data = fixture
              .Build<State>()
              .With(x => x.Title, fixture
                  .Build<Title>()
                  .With(x => x.References, [])
                  .Create())
              .With(x => x.Steps, new EnabledSteps(new Dictionary<IStep, StepInfo>()
                  { { A.Fake<UploadSakuraMangasStep>(), A.Fake<StepInfo>() } }))
              .Create();

        var result = await fixture
              .Create<StateValidator>()
              .TestValidateAsync(data, default, cancellationToken);

        result.ShouldHaveValidationErrorFor(prop => prop.Title)
              .WithErrorMessage("Não foi definida uma referência para a publicação da obra na Sakura Mangás. (Use a mesma da MangaDex)");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GivenGlobalPingTypeWithoutGlobalRoleDefinedShouldReturnInvalid(string? role)
    {
        fixture.FreezeFakeConfiguration(GlobalPing.GLOBAL_ROLE_KEY, role);

        var result = await fixture
              .Create<StateValidator>()
              .TestValidateAsync(data, default, cancellationToken);

        result.ShouldHaveValidationErrorFor(prop => prop)
              .WithErrorMessage("É necessário definir um valor para ping global no arquivo de configuração do Bot de Scans.");
    }

    [Fact]
    public async Task GivenGlobalPingTypeAndErrorTocheckIfRoleExistsShouldReturnInvalid()
    {
        A.CallTo(() => fixture
              .FreezeFake<RolesService>()
              .GetRoleAsync("some-role", cancellationToken))
              .Returns(Result.Fail(["err-1", "err-2"]));

        var result = await fixture
              .Create<StateValidator>()
              .TestValidateAsync(data, default, cancellationToken);

        result.ShouldHaveValidationErrorFor(prop => prop)
              .WithErrorMessage("err-1; err-2");
    }
}
