using BotDeScans.App.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO.Compression;
using System.Reflection;
namespace BotDeScans.UnitTests.Specs.Services;

public class FileServiceTests : UnitTest
{
    private readonly FileService service = new();

    public class GetMimeType : FileServiceTests
    {
        public class MimeTypeTestData : TheoryData<string, string>
        {
            public MimeTypeTestData() =>
                AddRange(FileService.MimeTypes.Select(x => (x.Key, x.Value)));
        }

        [Theory]
        [ClassData(typeof(MimeTypeTestData))]
        public void ShouldGetExpectedMimeType(string key, string value)
        {
            var filePath = fixture.Create<string>() + key;
            var result = service.GetMimeType(filePath);
            result.Should().Be(value);
        }
    }

    public class CreateZipFile : FileServiceTests, IDisposable
    {
        private static readonly string resourcesDirectory =
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                nameof(CreateZipFile) + "-resources-test");

        private static readonly string destinationDirectory =
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                nameof(CreateZipFile) + "-destination-test");

        public CreateZipFile()
        {
            if (Directory.Exists(destinationDirectory))
                Directory.Delete(destinationDirectory, true);

            if (Directory.Exists(resourcesDirectory))
                Directory.Delete(resourcesDirectory, true);

            Directory.CreateDirectory(resourcesDirectory);
            Directory.CreateDirectory(destinationDirectory);
        }

        [Fact]
        public void ShouldCreateZipInExpectedDirectory()
        {
            var result = service.CreateZipFile(
                "fileName",
                resourcesDirectory,
                destinationDirectory);

            File.Exists(result.Value).Should().BeTrue();
        }

        [Fact]
        public void ShouldReturnZipPath()
        {
            var expectedPath = Path.Combine(
                destinationDirectory,
                "fileName.zip");

            var result = service.CreateZipFile(
                "fileName",
                resourcesDirectory,
                destinationDirectory);

            result.Should().BeSuccess().And.HaveValue(expectedPath);
        }

        [Fact]
        public void ShouldReturnFailResultIfResourcesDirectoryIsSameThanDestinationDirectory()
        {
            var result = service.CreateZipFile(
                "fileName",
                resourcesDirectory.ToLower(),
                resourcesDirectory.ToUpper());

            result.Should().BeFailure().And.HaveError("Source and destination directories should not be the same.");
        }

        [Fact]
        public void ShouldContainsExpectedFilesInsideZipFile()
        {
            File.Create(Path.Combine(resourcesDirectory, "cover.png")).Dispose();
            File.Create(Path.Combine(resourcesDirectory, "credits.png")).Dispose();
            File.Create(Path.Combine(resourcesDirectory, "01.png")).Dispose();
            File.Create(Path.Combine(resourcesDirectory, "02.png")).Dispose();

            var result = service.CreateZipFile(
                "fileName",
                resourcesDirectory,
                destinationDirectory);

            using var zipFile = ZipFile.Open(result.Value, ZipArchiveMode.Read);

            var filesInsideZipFile = zipFile.Entries
                .Select(x => x.Name)
                .OrderBy(x => x)
                .Should().BeEquivalentTo(
                    ["1.png", "2.png", "3.png", "4.png"],
                    options => options.WithStrictOrdering());
        }

        [Fact]
        public void ShouldContainsExpectedFilesInsideZipFileCreatingPadingLeftZeroes()
        {
            File.Create(Path.Combine(resourcesDirectory, "cover.png")).Dispose();
            File.Create(Path.Combine(resourcesDirectory, "credits.png")).Dispose();
            File.Create(Path.Combine(resourcesDirectory, "01.png")).Dispose();
            File.Create(Path.Combine(resourcesDirectory, "02.png")).Dispose();
            File.Create(Path.Combine(resourcesDirectory, "03.png")).Dispose();
            File.Create(Path.Combine(resourcesDirectory, "04.png")).Dispose();
            File.Create(Path.Combine(resourcesDirectory, "05.png")).Dispose();
            File.Create(Path.Combine(resourcesDirectory, "06.png")).Dispose();
            File.Create(Path.Combine(resourcesDirectory, "07.png")).Dispose();
            File.Create(Path.Combine(resourcesDirectory, "08.png")).Dispose();

            var result = service.CreateZipFile(
                "fileName",
                resourcesDirectory,
                destinationDirectory);

            using var zipFile = ZipFile.Open(result.Value, ZipArchiveMode.Read);

            var filesInsideZipFile = zipFile.Entries
                .Select(x => x.Name)
                .OrderBy(x => x)
                .Should().BeEquivalentTo(
                    ["01.png", "02.png", "03.png", "04.png", "05.png", "06.png", "07.png", "08.png", "09.png", "10.png"],
                    options => options.WithStrictOrdering());
        }

        public void Dispose()
        {
            Directory.Delete(destinationDirectory, true);
            Directory.Delete(resourcesDirectory, true);
            GC.SuppressFinalize(this);
        }
    }

    public class CreatePdfFileAsync : FileServiceTests, IDisposable
    {
        private static readonly string resourcesDirectory =
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                nameof(CreatePdfFileAsync) + "-resources-test");

        private static readonly string destinationDirectory =
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                nameof(CreatePdfFileAsync) + "-destination-test");

        public CreatePdfFileAsync()
        {
            if (Directory.Exists(destinationDirectory))
                Directory.Delete(destinationDirectory, true);

            if (Directory.Exists(resourcesDirectory))
                Directory.Delete(resourcesDirectory, true);

            Directory.CreateDirectory(resourcesDirectory);
            Directory.CreateDirectory(destinationDirectory);
        }

        [Fact]
        public async Task ShouldCreatePdfInExpectedDirectory()
        {
            var imagePath = Path.Combine(resourcesDirectory, "file.png");
            using (var image = new Image<Rgba32>(1, 1))
            {
                await image.SaveAsync(imagePath, cancellationToken);
            }

            var result = await service.CreatePdfFileAsync(
                "fileName",
                resourcesDirectory,
                destinationDirectory);

            File.Exists(result.Value).Should().BeTrue();
        }

        [Fact]
        public async Task ShouldReturnPdfPath()
        {
            var expectedPath = Path.Combine(
                destinationDirectory,
                "fileName.pdf");

            var result = await service.CreatePdfFileAsync(
                "fileName",
                resourcesDirectory,
                destinationDirectory);

            result.Should().BeSuccess().And.HaveValue(expectedPath);
        }

        [Fact]
        public async Task ShouldReturnFailResultIfResourcesDirectoryIsSameThanDestinationDirectory()
        {
            var result = await service.CreatePdfFileAsync(
                "fileName",
                resourcesDirectory.ToLower(),
                resourcesDirectory.ToUpper());

            result.Should().BeFailure().And.HaveError("Source and destination directories should not be the same.");
        }

        public void Dispose()
        {
            Directory.Delete(destinationDirectory, true);
            Directory.Delete(resourcesDirectory, true);
            GC.SuppressFinalize(this);
        }
    }

    public class ReadTextFile : FileServiceTests, IDisposable
    {
        private static readonly string filePath =
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                nameof(ReadTextFile) + "-test.txt");

        public ReadTextFile()
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        [Fact]
        public void ShouldReadExpectedTextFromFile()
        {
            var expectedText = fixture.Create<string>();
            File.WriteAllText(filePath, expectedText);

            var result = service.ReadTextFile(filePath);

            result.Should().Be(expectedText);
        }

        public void Dispose()
        {
            File.Delete(filePath);
            GC.SuppressFinalize(this);
        }
    }
}
