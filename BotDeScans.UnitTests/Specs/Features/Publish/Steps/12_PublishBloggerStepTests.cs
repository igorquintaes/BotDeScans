using AutoFixture;
using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Services;
using BotDeScans.UnitTests.Extensions;
using BotDeScans.UnitTests.FakeObjects;
using Box.V2.Models;
using FakeItEasy;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Steps;

public class PublishBloggerStepTests : UnitTest
{
    private readonly IStep step;

    public PublishBloggerStepTests()
    {
        fixture.FreezeFake<IServiceProvider>();
        fixture.Inject(PublishStateBuilder.Create(fixture, StepEnum.PublishBlogspot));
        step = fixture.Create<PublishBloggerStep>();
    }

    public class Properties : PublishBloggerStepTests
    {
        [Fact]
        public void ShouldHaveExpectedName() =>
            step.StepName.Should().Be(StepEnum.PublishBlogspot);

        [Fact]
        public void ShouldHaveExpectedType() =>
            step.StepType.Should().Be(StepType.Publish);
    }

    public class ValidateBeforeFilesManagementAsync : PublishBloggerStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateBeforeFilesManagementAsync(cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ValidateAfterFilesManagementAsync : PublishBloggerStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateAfterFilesManagementAsync(cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ExecuteAsync : PublishBloggerStepTests
    {
        // todo: pending refactor
    }
}
