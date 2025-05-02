using BotDeScans.App.Features.Publish.State;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Reflection;
namespace BotDeScans.UnitTests.Specs.Features.Publish.Steps;

public class CompressFilesStepTests : UnitTest
{
    private readonly CompressFilesStep step;

    public CompressFilesStepTests()
    {
        fixture.Freeze<PublishState>();
        fixture.FreezeFake<ImageService>();
        step = fixture.Create<CompressFilesStep>();
    }

    public class Properties : CompressFilesStepTests
    {
        [Fact]
        public void ShouldHaveExpectedType() =>
            step.Type.Should().Be(StepType.Management);

        [Fact]
        public void ShouldHaveExpectedName() =>
            step.Name.Should().Be(StepName.Compress);

        [Fact]
        public void ShouldHaveExpectedIsMandatory() =>
            step.IsMandatory.Should().Be(true);
    }

    public class ExecuteAsync : CompressFilesStepTests, IDisposable
    {
        private readonly string resourcesDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            nameof(CompressFilesStepTests));

        public ExecuteAsync()
        {
            if (Directory.Exists(resourcesDirectory))
                Directory.Delete(resourcesDirectory, true);

            Directory.CreateDirectory(resourcesDirectory);
        }

        [Fact]
        public async Task GivenStateFolderShouldProccessEachFileInside()
        {
            var firstFilePath = Path.Combine(resourcesDirectory, "01.png");
            var secondFilePath = Path.Combine(resourcesDirectory, "02.png");
            foreach (var filePath in new[] { firstFilePath, secondFilePath })
            {
                using var image = new Image<Rgba32>(1, 1);
                image.Mutate(x => x.BackgroundColor(new Rgba32(5, 5, 5)));
                await image.SaveAsync(filePath, cancellationToken);
            }

            fixture.Freeze<PublishState>().InternalData.OriginContentFolder = resourcesDirectory;

            A.CallTo(() => fixture
                .FreezeFake<ImageService>()
                .IsGrayscale(firstFilePath, 20))
                .Returns(true);

            A.CallTo(() => fixture
                .FreezeFake<ImageService>()
                .IsGrayscale(secondFilePath, 20))
                .Returns(false);

            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeSuccess();

            A.CallTo(() => fixture
                .FreezeFake<ImageService>()
                .CompressImageAsync(
                    firstFilePath,
                    true,
                    A<CancellationToken>.That.Matches(ct => ct != CancellationToken.None)))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<ImageService>()
                .CompressImageAsync(
                    secondFilePath,
                    false,
                    A<CancellationToken>.That.Matches(ct => ct != CancellationToken.None)))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<ImageService>()
                .CompressImageAsync(
                    A<string>.Ignored,
                    A<bool>.Ignored,
                    A<CancellationToken>.That.Matches(ct => ct != CancellationToken.None)))
                .MustHaveHappenedTwiceExactly();
        }

        public void Dispose()
        {
            Directory.Delete(resourcesDirectory, true);
            GC.SuppressFinalize(this);
        }
    }
}
