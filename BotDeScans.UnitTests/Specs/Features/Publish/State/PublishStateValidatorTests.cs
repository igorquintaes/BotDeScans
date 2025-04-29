using BotDeScans.App.Features.Publish.State.Models;
using BotDeScans.App.Models;
using BotDeScans.App.Services.Discord;
using FluentValidation;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.UnitTests.Specs.Features.Publish.State;

public class PublishStateValidatorTests : UnitTest
{
    public PublishStateValidatorTests()
    {
        fixture.FreezeFake<RolesService>();
        fixture.FreezeFake<IConfiguration>();
        fixture.FreezeFake<IValidator<Info>>();
        fixture.FreezeFake<IValidator<Title>>();
    }

    [Fact]
    public async Task ToDo() => throw new NotImplementedException();
}
