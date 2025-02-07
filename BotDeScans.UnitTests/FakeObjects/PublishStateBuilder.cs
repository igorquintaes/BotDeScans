using AutoFixture;
using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.UnitTests.Extensions;
using System.Linq;

namespace BotDeScans.UnitTests.FakeObjects;

public static class PublishStateBuilder
{
    public static PublishState Create(IFixture fixture, params StepEnum[] steps)
    {
        fixture.FreezeFakeConfiguration(
            key: "Settings:Publish:Steps", 
            values: steps.Select(x => x.ToString()));

        return fixture.Create<PublishState>();
    }
}
