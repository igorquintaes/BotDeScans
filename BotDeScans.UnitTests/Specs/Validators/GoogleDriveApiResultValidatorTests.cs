﻿using AutoBogus;
using BotDeScans.App.Services;
using BotDeScans.App.Validators;
using FakeItEasy;
using FluentAssertions;
using FluentAssertions.Execution;
using Google.Apis.Drive.v3.Data;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BotDeScans.UnitTests.Specs.Validators
{
    public class GoogleDriveApiResultValidatorTests : UnitTest<GoogleDriveApiResultValidator>
    {
        private readonly ChapterValidatorService chapterValidatorService;
        private readonly ExtractionService extractionService;
        private readonly FileList googleDriveApiResult;

        public GoogleDriveApiResultValidatorTests()
        {
            googleDriveApiResult = AutoFaker.Generate<FileList>();
            chapterValidatorService = A.Fake<ChapterValidatorService>();
            extractionService = A.Fake<ExtractionService>();
            A.CallTo(chapterValidatorService)
                .WithReturnType<bool>()
                .Returns(true);

            instance = new (chapterValidatorService);
        }

        [Fact]
        public void ShouldBeValidWhenNoErrorsOccurs() => 
            instance.Validate(googleDriveApiResult).IsValid.Should().BeTrue();

        [Theory]
        [MemberData(nameof(chapterValidations))]
        public void ShouldBeInvalidIfShouldHaveOnlyFilesReturnsFalse(
            string chapterValidationMethodName, 
            string errorMessage)
        {
            A.CallTo(chapterValidatorService)
                .Where(x => x.Method.Name == chapterValidationMethodName)
                .WithReturnType<bool>()
                .Returns(false);

            var validationResult = instance.Validate(googleDriveApiResult);

            using var _ = new AssertionScope();
            validationResult.IsValid.Should().BeFalse();
            validationResult.Errors.Should().HaveCount(1);
            validationResult.Errors.FirstOrDefault()?.ErrorMessage
                .Should().Be(errorMessage);
        }

        [Fact]
        public void ShouldBeAbleToVerifyAndReturnAllPossibleErrors()
        {
            A.CallTo(chapterValidatorService)
                .WithReturnType<bool>()
                .Returns(false);

            var validationResult = instance.Validate(googleDriveApiResult);
            var errorMessages = validationResult.Errors?.Select(x => x.ErrorMessage);
            var expectedErrorMessages = chapterValidations.Select(x => x.As<object[]>().Last().ToString());

            using var _ = new AssertionScope();
            validationResult.IsValid.Should().BeFalse();
            errorMessages.Should().BeEquivalentTo(expectedErrorMessages, 
                options => options.WithStrictOrdering());
        }

        public static readonly IEnumerable<object[]> chapterValidations =
            new List<object[]>
            {
                new object[]
                {
                    nameof(ChapterValidatorService.ShouldHaveOnlyFiles),
                    "O diretório precisa conter apenas arquivos."
                },
                new object[]
                {
                    nameof(ChapterValidatorService.ShouldHaveExactlyOneCoverFile),
                    "O diretório precisa conter apenas uma única página de capa, toda em minúsculo. (ex: capa.extensão)"
                },
                new object[]
                {
                    nameof(ChapterValidatorService.ShouldHaveExactlyOneCreditsFile),
                    "O diretório precisa conter apenas uma única página de créditos, toda em minúsculo. (ex: creditos.extensão)"
                },
                new object[]
                {
                    nameof(ChapterValidatorService.ShouldHaveOnlySupportedFileExtensions),
                    $"O diretório precisa conter apenas arquivos com as extensões esperadas: {string.Join("", FileReleaseService.ValidCoverFiles)}."
                },
                new object[]
                {
                    nameof(ChapterValidatorService.ShouldHaveOrderedDoublePages),
                    "As páginas duplas precisam estar numeradas em ordem e sequencialmente."
                },
                new object[]
                {
                    nameof(ChapterValidatorService.ShouldHaveNotAnySkippedPage),
                    "As páginas precisam ter números sequenciais, sem pular números."
                },
                new object[]
                {
                    nameof(ChapterValidatorService.ShouldHaveSamePageLength),
                    "O nome dos arquivos das páginas precisa ser escrito de modo que todos tenham o mesmo tamanho (dica: use zero à esqueda)."
                },
                new object[]
                {
                    nameof(ChapterValidatorService.ShouldStartInPageOne),
                    "A primeira página deve começar com o número 1 (1, 01, 001...)."
                },
                new object[]
                {
                    nameof(ChapterValidatorService.ShouldNotHaveAnyTextPageThanCoverAndCredits),
                    "Não deve conter outras páginas senão numerais, créditos e capa."
                },
            };
    }
}
