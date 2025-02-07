using AutoFixture;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.UnitTests.FakeObjects;
using FluentAssertions;
using Xunit;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Steps;

public class UploadZipMegaStepTests : UnitTest
{
    private readonly IStep step;

    public UploadZipMegaStepTests()
    {
        fixture.Inject(PublishStateBuilder.Create(fixture, StepEnum.UploadZipMega));
        step = fixture.Create<UploadZipMegaStep>();
    }

    public class Properties : UploadZipMegaStepTests
    {
        [Fact]
        public void ShouldHaveExpectedName() =>
            step.StepName.Should().Be(StepEnum.UploadZipMega);

        [Fact]
        public void ShouldHaveExpectedType() =>
            step.StepType.Should().Be(StepType.Publish);
    }

    public class ValidateBeforeFilesManagementAsync : UploadZipMegaStepTests
    {

    }

    public class ValidateAfterFilesManagementAsync : UploadZipMegaStepTests
    {

    }

    public class ExecuteAsync : UploadZipMegaStepTests
    {

    }
}
