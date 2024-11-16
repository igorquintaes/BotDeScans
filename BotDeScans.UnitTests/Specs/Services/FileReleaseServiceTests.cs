using BotDeScans.App.Services;
using FluentAssertions;
using FluentAssertions.Execution;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;
namespace BotDeScans.UnitTests.Specs.Services;

public class FileReleaseServiceTests : UnitTest<FileReleaseService>, IDisposable
{
    //private readonly string fileReleaseDirectory = Path.Combine(
    //    FileReleaseService.RootDirectory,
    //    FileReleaseService.BaseDirectory);

    //public FileReleaseServiceTests()
    //{
    //    CleanFileReleaseBaseDirectory();
    //    instance = new();
    //}

    //public override void Dispose()
    //{
    //    instance?.Dispose();
    //    CleanFileReleaseBaseDirectory();
    //    base.Dispose();

    //    GC.SuppressFinalize(this);
    //}

    //public class Constructor : FileReleaseServiceTests
    //{
    //    [Fact]
    //    public void ShouldCreateReleasesDirectory() => 
    //        Directory.Exists(fileReleaseDirectory).Should().BeTrue();

    //    [Theory]
    //    [InlineData(null, nameof(FileReleaseService.ReleaseDirectory))]
    //    [InlineData(".zip", nameof(FileReleaseService.ZipDirectory))]
    //    [InlineData(".pdf", nameof(FileReleaseService.PdfDirectory))]
    //    [InlineData(".cover", nameof(FileReleaseService.CoverDirectory))]
    //    public void ShouldCreateReleaseDirectories(string extension, string propertyName)
    //    {
    //        var releaseDirectory = Directory
    //            .GetDirectories(fileReleaseDirectory)
    //            .Where(x => Path.GetFileName(x) == Path.GetFileName(instance.ReleaseDirectory) + extension)
    //            .ToList();

    //        var expectedReleaseDir = instance
    //            .GetType()
    //            .GetProperty(propertyName)!
    //            .GetValue(instance, null)!
    //            .ToString();

    //        var couldParseDirName = DateTime.TryParseExact(
    //            Path.GetFileName(releaseDirectory.First()).Replace(extension ?? dataGenerator.Random.String(), ""),
    //            FileReleaseService.UniqueReleaseDirectoryFormat,
    //            CultureInfo.InvariantCulture,
    //            DateTimeStyles.None,
    //            out var directoryNameAsDateTime);

    //        using var _ = new AssertionScope();
    //        releaseDirectory.Should().ContainSingle();
    //        releaseDirectory.Single().Should().Be(expectedReleaseDir);
    //        couldParseDirName.Should().BeTrue();
    //        directoryNameAsDateTime.Should().BeCloseTo(
    //            DateTime.Now, TimeSpan.FromSeconds(1));
    //    }
    //}

    //public class ManageCoverFile : FileReleaseServiceTests
    //{
    //    public readonly static IEnumerable<object[]> ValidCoverFiles = 
    //        FileReleaseService.ValidCoverFiles.Select(x => new object[] { x });

    //    [Fact]
    //    public void ShouldThrownInvalidOperationExceptionIfThereIsAlreadyCoverFilesOnCoverFolder()
    //    {
    //        var filePath = Path.Combine(instance.CoverDirectory, "file.extension");
    //        File.Create(filePath).Dispose();
    //        Action action = () => instance.ManageCoverFile();
    //        action.Should().ThrowExactly<InvalidOperationException>()
    //            .WithMessage("Cover files were already managed.");
    //    }

    //    [Fact]
    //    public void ShouldReturnNullIfDontFindAnyCoverFileInsideReleaseFolder() => 
    //        instance.ManageCoverFile().Should().BeNull();

    //    [Theory]
    //    [MemberData(nameof(ValidCoverFiles))]
    //    public void ShouldReturnCoverFilePathAfterMovingItToCoverDirectory(string coverFileName)
    //    {
    //        var previousPath = Path.Combine(instance.ReleaseDirectory, coverFileName);
    //        var newPath = Path.Combine(instance.CoverDirectory, coverFileName);
    //        File.Create(previousPath).Dispose();

    //        var result = instance.ManageCoverFile();

    //        using var _ = new AssertionScope();
    //        result.Should().Be(newPath);
    //        File.Exists(newPath).Should().BeTrue();
    //        File.Exists(previousPath).Should().BeFalse();
    //    }

    //    [Fact]
    //    public void ShouldNotMoveAnyFileThatIsNotACoverFile()
    //    {
    //        var originalFilePath = Path.Combine(instance.ReleaseDirectory, "file.extension");
    //        var movedFilePath = Path.Combine(instance.CoverDirectory, "file.extension");
    //        File.Create(originalFilePath).Dispose();

    //        var result = instance.ManageCoverFile();

    //        using var _ = new AssertionScope();
    //        result.Should().BeNull();
    //        File.Exists(originalFilePath).Should().BeTrue();
    //        File.Exists(movedFilePath).Should().BeFalse();
    //    }
    //}

    //public class DisposeCall : FileReleaseServiceTests
    //{
    //    public DisposeCall() => 
    //        instance.Dispose();

    //    [Theory]
    //    [InlineData(nameof(FileReleaseService.ZipDirectory))]
    //    [InlineData(nameof(FileReleaseService.PdfDirectory))]
    //    [InlineData(nameof(FileReleaseService.CoverDirectory))]
    //    [InlineData(nameof(FileReleaseService.ReleaseDirectory))]
    //    public void ShouldDeleteReleaseDirectories(string propertyName)
    //    {
    //        var expectedReleaseDir = instance
    //            .GetType()
    //            .GetProperty(propertyName)!
    //            .GetValue(instance, null)!
    //            .ToString()!;

    //        Action getDirectories = () => Directory.GetDirectories(expectedReleaseDir);
    //        getDirectories.Should().ThrowExactly<DirectoryNotFoundException>();
    //    }
    //}

    //private static void CleanFileReleaseBaseDirectory()
    //{
    //    var releasesDirectory = Path.Combine(
    //        FileReleaseService.RootDirectory, 
    //        FileReleaseService.BaseDirectory);

    //    if (Directory.Exists(releasesDirectory))
    //        Directory.Delete(releasesDirectory, true);
    //}
}
