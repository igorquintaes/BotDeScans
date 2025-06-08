using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using FluentAssertions.Execution;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class StepsServiceTests : UnitTest
{
    private readonly StepsService service;

    public StepsServiceTests()
    {
        fixture.FreezeFakeConfiguration(
            key: StepsService.STEPS_KEY,
            values:
            [
                StepName.UploadZipGoogleDrive.ToString(),
                StepName.UploadMangadex.ToString()
            ]);

        var allsteps = Assembly
            .GetAssembly(typeof(IStep))!
            .GetTypes()
            .Where(type => type.GetInterface(nameof(IStep)) is not null
                        && type.IsInterface is false)
            .Select(type => (IStep)RuntimeHelpers.GetUninitializedObject(type)!);

        fixture.Inject(allsteps);

        service = fixture.Create<StepsService>();
    }

    [Fact]
    public void GivenSuccessfulExecutionShouldReturnExpectedEnablesSteps()
    {
        var result = service.GetEnabledSteps();

        using var _ = new AssertionScope();
        result.Should().HaveCount(6);

        result.ElementAt(0).Key.Name.Should().Be(StepName.Setup);
        result.ElementAt(1).Key.Name.Should().Be(StepName.Download);
        result.ElementAt(2).Key.Name.Should().Be(StepName.Compress);
        result.ElementAt(3).Key.Name.Should().Be(StepName.ZipFiles);
        result.ElementAt(4).Key.Name.Should().Be(StepName.UploadZipGoogleDrive);
        result.ElementAt(5).Key.Name.Should().Be(StepName.UploadMangadex);

        result.ElementAt(0).Value.Status.Should().Be(StepStatus.QueuedForExecution);
        result.ElementAt(1).Value.Status.Should().Be(StepStatus.QueuedForExecution);
        result.ElementAt(2).Value.Status.Should().Be(StepStatus.QueuedForExecution);
        result.ElementAt(3).Value.Status.Should().Be(StepStatus.QueuedForExecution);
        result.ElementAt(4).Value.Status.Should().Be(StepStatus.QueuedForValidation);
        result.ElementAt(5).Value.Status.Should().Be(StepStatus.QueuedForValidation);
    }
}
