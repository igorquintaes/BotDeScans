using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.Steps;
namespace BotDeScans.UnitTests.Specs.Features.Publish.Steps;

public class PublishBloggerStepTests : UnitTest
{
    private readonly IStep step;

    public PublishBloggerStepTests()
    {
        fixture.Freeze<PublishState>();
        fixture.FreezeFake<IServiceProvider>();
        fixture.FreezeFakeConfiguration("Blogger:Url", "www.escoladescans.com");
        fixture.FreezeFakeConfiguration("Blogger:Id", fixture.Create<string>());

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
        [Fact(Skip = "Pending improve initialization, will change current method logic")]
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
