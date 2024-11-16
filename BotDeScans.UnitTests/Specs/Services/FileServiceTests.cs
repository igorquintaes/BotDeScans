using BotDeScans.App.Services;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;
namespace BotDeScans.UnitTests.Specs.Services;

public class FileServiceTests : UnitTest<FileService>
{
    public FileServiceTests() =>
        instance = new ();

    public class GetMimeType : FileServiceTests
    {
        public readonly static IEnumerable<object[]> MimeTypes =
            FileService.MimeTypes.Select(x => new object[] { x });

        [Theory]
        [MemberData(nameof(FileService.MimeTypes))]
        public void ShouldGetExpectedMimeType(KeyValuePair<string, string> mimeType)
        {
            var filePath = dataGenerator.System.FilePath() + mimeType.Key;
            var result = instance.GetMimeType(filePath);
            result.Should().Be(mimeType.Value);
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

        private const string zipFileName = "file.zip";

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
            instance.CreateZipFile(
                zipFileName,
                resourcesDirectory,
                destinationDirectory);

            File.Exists(Path.Combine(destinationDirectory, zipFileName)).Should().BeTrue();
        }

        [Fact]
        public void ShouldThrowExceptionIfFileNameExtensionIsNotZip()
        {
            Action action = () => instance.CreateZipFile(
                "file.extension",
                resourcesDirectory,
                destinationDirectory);

            action.Should().ThrowExactly<ArgumentException>()
                .WithMessage("Desired zip file name must has .zip extension.");
        }

        [Fact]
        public void ShouldThrowExceptionIfResourcesDirectoryIsSameThanDestinationDirectory()
        {
            Action action = () => instance.CreateZipFile(
                zipFileName,
                resourcesDirectory.ToUpper(),
                resourcesDirectory.ToLower());

            action.Should().ThrowExactly<ArgumentException>()
                .WithMessage("Source and destination directories should not be the same.");
        }

        [Fact]
        public void ShouldContainsExpectedFilesInsideZipFile()
        {
            File.Create(Path.Combine(resourcesDirectory, "01.png")).Dispose();
            File.Create(Path.Combine(resourcesDirectory, "02.png")).Dispose();

            instance.CreateZipFile(
                zipFileName,
                resourcesDirectory,
                destinationDirectory);

            using var zipFile = ZipFile.Open(Path.Combine(destinationDirectory, zipFileName), ZipArchiveMode.Read);
            var filesInsideZipFile = zipFile.Entries.Select(x => x.Name);
            filesInsideZipFile.Should().BeEquivalentTo(
                new[] { "01.png", "02.png" }, 
                options => options.WithStrictOrdering());
        }

        public override void Dispose()
        {
            Directory.Delete(destinationDirectory, true);
            Directory.Delete(resourcesDirectory, true);
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public class CreateFileAsync : FileServiceTests, IDisposable
    {
        private static readonly string fileName =
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "file-name.txt");

        [Fact]
        public async Task ShouldCreateExpectedFile()
        {
            var fileContentAsString = dataGenerator.Lorem.Paragraph();
            var fileContentAsByteArray = Encoding.UTF8.GetBytes(fileContentAsString);

            await instance.CreateFileAsync(fileContentAsByteArray, fileName);

            File.ReadAllText(fileName, Encoding.UTF8).Should().Be(fileContentAsString);
        }

        public override void Dispose()
        {
            if (File.Exists(fileName))
                File.Delete(fileName);

            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
