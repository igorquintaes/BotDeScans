using BotDeScans.App.Services;
using FluentAssertions.Execution;
using System.Reflection;

namespace BotDeScans.UnitTests.Specs.Services;

public class FileReleaseServiceTests : UnitTest
{
    private readonly FileReleaseService service = new();

    public class MoveCoverFile : FileReleaseServiceTests, IDisposable
    {
        private static readonly string fromDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "from-test");

        private static readonly string toDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "to-test");

        [Fact]
        public void ShouldMoveCoverFileFromDirectoryToOther()
        {
            var pageFilePath = Path.Combine(fromDirectory, "01.png");
            var coverFilePath = Path.Combine(fromDirectory, $"{FileReleaseService.COVER_PAGE_NAME}.png");
            var expectedCoverFilePath = Path.Combine(toDirectory, $"{FileReleaseService.COVER_PAGE_NAME}.png");
            Directory.CreateDirectory(fromDirectory);
            Directory.CreateDirectory(toDirectory);
            File.Create(pageFilePath).Dispose();
            File.Create(coverFilePath).Dispose();

            var result = service.MoveCoverFile(fromDirectory, toDirectory);

            using var _ = new AssertionScope();
            result.Should().Be(expectedCoverFilePath);
            Directory.GetFiles(fromDirectory).Should().HaveCount(1);
            Directory.GetFiles(toDirectory).Should().HaveCount(1);
            Directory.GetFiles(fromDirectory).FirstOrDefault().Should().Be(pageFilePath);
            Directory.GetFiles(toDirectory).FirstOrDefault().Should().Be(expectedCoverFilePath);
        }

        public void Dispose()
        {
            Directory.Delete(fromDirectory, true);
            Directory.Delete(toDirectory, true);
            GC.SuppressFinalize(this);
        }
    }

    public class CreateScopedDirectory : FileReleaseServiceTests, IDisposable
    {
        [Fact]
        public void GivenRequestShouldCreateTemporaryDirectory()
        {
            var directoryResult = service.CreateScopedDirectory();

            using var _ = new AssertionScope();
            Directory.Exists(directoryResult).Should().BeTrue();
        }

        public void Dispose()
        {
            service.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public class DisposeMethod : FileReleaseServiceTests
    {
        [Fact]
        public void GivenDisposeShouldDeleteTemporaryDirectories()
        {
            var directoryResult = service.CreateScopedDirectory();
            service.Dispose();

            // scoped folder
            Directory.Exists(directoryResult).Should().BeFalse();

            // scoped base folder
            Directory.Exists(Path.Combine(directoryResult, "..")).Should().BeFalse();
        }

        [Fact]
        public void GivenNoneTemporatyDirectoriesShouldCallDisposeWithoutErrors()
            => service.Dispose();
    }
}
