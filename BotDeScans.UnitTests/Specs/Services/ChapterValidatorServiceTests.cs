using BotDeScans.App.Services;
using FluentAssertions;
using Google.Apis.Drive.v3.Data;
using System.Collections.Generic;
using Xunit;

namespace BotDeScans.UnitTests.Specs.Services
{
    public class ChapterValidatorServiceTests
    {
        private static readonly ChapterValidatorService validatorService = new();
        private readonly FileList validModel;

        public ChapterValidatorServiceTests() => 
            validModel = new FileList
            {
                Files = new List<File>
                {
                    new File
                    {
                        Kind = "drive#file",
                        Name = "01.png",
                    },
                    new File
                    {
                        Kind = "drive#file",
                        Name = "02.png",
                    },
                    new File
                    {
                        Kind = "drive#file",
                        Name = "03-04.png",
                    },
                    new File
                    {
                        Kind = "drive#file",
                        Name = "05.png",
                    },
                    new File
                    {
                        Kind = "drive#file",
                        Name = "capa.png",
                    },
                    new File
                    {
                        Kind = "drive#file",
                        Name = "creditos.png",
                    },
                }
            };

        public class ShouldHaveOnlyFiles : ChapterValidatorServiceTests
        {
            [Fact]
            public void IsValid() =>
                validatorService.ShouldHaveOnlyFiles(validModel).Should().BeTrue();

            [Fact]
            public void IsInvalid()
            {
                validModel.Files[0].Kind = "drive#folder";
                validatorService.ShouldHaveOnlyFiles(validModel).Should().BeFalse();
            }
        }

        public class ShouldHaveExactlyOneCoverFile : ChapterValidatorServiceTests
        {
            [Fact]
            public void IsValid() =>
                validatorService.ShouldHaveExactlyOneCoverFile(validModel).Should().BeTrue();

            [Theory]
            [InlineData("capa.png")]
            [InlineData("capa.jpg")]
            [InlineData("capa.jpeg")]
            public void IsInvalid(string coverPageName)
            {
                validModel.Files[0].Name = coverPageName;
                validatorService.ShouldHaveExactlyOneCoverFile(validModel).Should().BeFalse();
            }
        }

        public class ShouldHaveExactlyOneCreditsFile : ChapterValidatorServiceTests
        {
            [Fact]
            public void IsValid() =>
                validatorService.ShouldHaveExactlyOneCreditsFile(validModel).Should().BeTrue();

            [Theory]
            [InlineData("creditos.png")]
            [InlineData("creditos.jpg")]
            [InlineData("creditos.jpeg")]
            public void IsInvalid(string creditPageName)
            {
                validModel.Files[0].Name = creditPageName;
                validatorService.ShouldHaveExactlyOneCreditsFile(validModel).Should().BeFalse();
            }
        }

        public class ShouldHaveOnlySupportedFileExtensions : ChapterValidatorServiceTests
        {
            [Fact]
            public void IsValid() =>
                validatorService.ShouldHaveOnlySupportedFileExtensions(validModel).Should().BeTrue();

            [Theory]
            [InlineData("zip")]
            [InlineData("rar")]
            [InlineData("pdf")]
            [InlineData("exe")]
            [InlineData("bat")]
            [InlineData("jpg.exe")]
            public void IsInvalid(string unsupportedExtension)
            {
                validModel.Files[0].Name = $"01.{unsupportedExtension}";
                validatorService.ShouldHaveOnlySupportedFileExtensions(validModel).Should().BeFalse();
            }
        }

        public class ShouldHaveOrderedDoublePages : ChapterValidatorServiceTests
        {
            [Fact]
            public void IsValid() =>
                validatorService.ShouldHaveOrderedDoublePages(validModel).Should().BeTrue();

            [Theory]
            [InlineData("01-03.png")]
            [InlineData("03-03.png")]
            [InlineData("03-02.png")]
            [InlineData("03-XX.png")]
            [InlineData("XX-03.png")]
            [InlineData("01--02.png")]
            [InlineData("01-02-03.png")]
            public void IsInvalid(string unordenedPageName)
            {
                validModel.Files[0].Name = unordenedPageName;
                validatorService.ShouldHaveOrderedDoublePages(validModel).Should().BeFalse();
            }
        }

        public class ShouldHaveNotAnySkippedPage : ChapterValidatorServiceTests
        {
            [Fact]
            public void IsValid() =>
                validatorService.ShouldHaveNotAnySkippedPage(validModel).Should().BeTrue();

            [Fact]
            public void IsInvalid()
            {
                validModel.Files[3].Name = "06.png";
                validatorService.ShouldHaveNotAnySkippedPage(validModel).Should().BeFalse();
            }
        }

        public class ShouldNotHaveAnyTextPageThanCoverAndCredits : ChapterValidatorServiceTests
        {
            [Fact]
            public void IsValid() =>
                validatorService.ShouldNotHaveAnyTextPageThanCoverAndCredits(validModel).Should().BeTrue();

            [Fact]
            public void IsInvalid()
            {
                validModel.Files[3].Name = "error.png";
                validatorService.ShouldNotHaveAnyTextPageThanCoverAndCredits(validModel).Should().BeFalse();
            }
        }

        public class ShouldHaveSamePageLength : ChapterValidatorServiceTests
        {
            [Fact]
            public void IsValid() =>
                validatorService.ShouldHaveSamePageLength(validModel).Should().BeTrue();

            [Fact]
            public void IsInvalid()
            {
                validModel.Files[0].Name = "001.png";
                validatorService.ShouldHaveSamePageLength(validModel).Should().BeFalse();
            }
        }

        public class ShouldStartInPageOne : ChapterValidatorServiceTests
        {
            [Fact]
            public void IsValid() =>
                validatorService.ShouldStartInPageOne(validModel).Should().BeTrue();

            [Fact]
            public void IsInvalidWithNumbers()
            {
                validModel.Files[0].Name = "06.png";
                validatorService.ShouldStartInPageOne(validModel).Should().BeFalse();
            }
            [Fact]
            public void IsInvalidWithRandomString()
            {
                validModel.Files[0].Name = "error.png";
                validatorService.ShouldStartInPageOne(validModel).Should().BeFalse();
            }
        }
    }
}
