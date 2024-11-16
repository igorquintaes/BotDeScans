using BotDeScans;
using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Services;
using BotDeScans.UnitTests;
using BotDeScans.UnitTests.Specs;
using BotDeScans.UnitTests.Specs.Features.GoogleDrive;
using BotDeScans.UnitTests.Specs.Services;
using FluentAssertions;
using FluentValidation.TestHelper;
using Google.Apis.Drive.v3.Data;
using System.Collections.Generic;
using Xunit;
namespace BotDeScans.UnitTests.Specs.Features.GoogleDrive;

public class FileListValidatorTests
{
    private readonly FileListValidator validator = new FileListValidator();
    private readonly FileList data = new FileList
    {
        Files = new List<File>
        {
            new File { Kind = "drive#file", Name = "01.png" },
            new File { Kind = "drive#file", Name = "02.png" },
            new File { Kind = "drive#file", Name = "03-04.png" },
            new File { Kind = "drive#file", Name = "05.png" },
            new File { Kind = "drive#file", Name = "capa.png" },
            new File { Kind = "drive#file", Name = "creditos.png" }
        }
    };

    [Fact]
    public void ShouldBeValid() => 
        validator.TestValidate(data).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void ShouldBeInvalidIfContainsFolderValue()
    {
        data.Files[0].Kind = "drive#folder";
        validator.TestValidate(data)
            .ShouldHaveAnyValidationError()
            .WithErrorMessage("O diretório precisa conter apenas arquivos.");
    }

    [Fact]
    public void ShouldBeInvalidIfContainsMoreThanOneCoverFile()
    {
        data.Files[0].Name = "capa.png";
        validator.TestValidate(data)
            .ShouldHaveAnyValidationError()
            .WithErrorMessage("O diretório precisa conter apenas uma única página de capa. (ex: capa.extensão)");
    }

    [Fact]
    public void ShouldBeInvalidIfContainsMoreThanOneCreditsFile()
    {
        data.Files[0].Name = "creditos.png";
        validator.TestValidate(data)
            .ShouldHaveAnyValidationError()
            .WithErrorMessage("O diretório precisa conter apenas uma única página de créditos. (ex: creditos.extensão)");
    }

    [Theory]
    [InlineData("zip")]
    [InlineData("rar")]
    [InlineData("pdf")]
    [InlineData("exe")]
    [InlineData("bat")]
    [InlineData("jpg.exe")]
    public void ShouldBeInvalidIfContainsAnyFileWithUnexpectedExtension(string unsupportedExtension)
    {
        data.Files[0].Name = $"01.{unsupportedExtension}";
        validator.TestValidate(data)
            .ShouldHaveAnyValidationError()
            .WithErrorMessage($"O diretório precisa conter apenas arquivos com as extensões esperadas: {string.Join("", FileReleaseService.ValidCoverFiles)}.");
    }

    [Theory]
    [InlineData("error.png")]
    [InlineData("06-06B.png")]
    [InlineData("06A-06B.png")]
    [InlineData("06-07-08.png")]
    public void ShouldBeInvalidIfContainsAnyTextPageThanCoverAndCredits(string pageName)
    {
        data.Files[3].Name = pageName;
        validator.TestValidate(data)
            .ShouldHaveAnyValidationError()
            .WithErrorMessage("Não deve conter outras páginas senão numerais, créditos e capa.");
    }

    [Fact]
    public void ShouldBeInvalidIfFirstPageIsNotOne()
    {
        data.Files[0].Name = "06.png";
        validator.TestValidate(data)
            .ShouldHaveAnyValidationError()
            .WithErrorMessage("A primeira página deve começar com o número 1 (1, 01, 001...). Isso também vale para página dupla (1-2, 01-02, 001-002...).");
    }

    [Theory]
    [InlineData("05-07.png")]
    [InlineData("06-05.png")]
    public void ShouldBeInvalidIfDoublePagesAreNotNumberOrdered(string pageName)
    {
        data.Files[3].Name = pageName;
        validator.TestValidate(data)
            .ShouldHaveAnyValidationError()
            .WithErrorMessage("As páginas duplas precisam estar numeradas em ordem e sequencialmente.");
    }

    [Fact]
    public void ShouldBeInvalidIfSkipsSomePage()
    {
        data.Files[3].Name = "06.png";
        validator.TestValidate(data)
            .ShouldHaveAnyValidationError()
            .WithErrorMessage("As páginas precisam ter números sequenciais, sem pular números.");
    }

    [Fact]
    public void ShouldBeInvalidIfContainsAnyPageNumberWithDifferentLength()
    {
        data.Files[0].Name = "001.png";
        validator.TestValidate(data)
            .ShouldHaveAnyValidationError()
            .WithErrorMessage("O nome dos arquivos das páginas precisa ser escrito de modo que todos tenham o mesmo tamanho (dica: use zero à esqueda).");
    }
}
