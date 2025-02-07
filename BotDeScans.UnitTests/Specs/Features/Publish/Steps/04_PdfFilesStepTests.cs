using AutoFixture;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.UnitTests.FakeObjects;
using FluentAssertions;
using Xunit;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Steps;

public class PdfFilesStepTests : UnitTest
{
    private readonly IStep step;

    public PdfFilesStepTests()
    {
        fixture.Inject(PublishStateBuilder.Create(fixture, StepEnum.PdfFiles));
        step = fixture.Create<PdfFilesStep>();
    }

    public class Properties : PdfFilesStepTests
    {
        [Fact]
        public void ShouldHaveExpectedName() =>
            step.StepName.Should().Be(StepEnum.PdfFiles);

        [Fact]
        public void ShouldHaveExpectedType() =>
            step.StepType.Should().Be(StepType.Management);
    }

    public class ValidateBeforeFilesManagementAsync : PdfFilesStepTests
    {

    }

    public class ValidateAfterFilesManagementAsync : PdfFilesStepTests
    {

    }

    public class ExecuteAsync : PdfFilesStepTests
    {

    }
}
