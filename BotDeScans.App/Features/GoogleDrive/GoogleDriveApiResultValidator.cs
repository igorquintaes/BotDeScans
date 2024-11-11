using BotDeScans.App.Services;
using FluentValidation;
using Google.Apis.Drive.v3.Data;
namespace BotDeScans.App.Features.GoogleDrive;

public class GoogleDriveApiResultValidator : AbstractValidator<FileList>
{
    public GoogleDriveApiResultValidator(ValidatorService chapterValidatorService) =>
        RuleFor(model => model)
            .Must(chapterValidatorService.ShouldHaveOnlyFiles)
                .WithMessage("O diretório precisa conter apenas arquivos.")
            .Must(chapterValidatorService.ShouldHaveExactlyOneCoverFile)
                .WithMessage("O diretório precisa conter apenas uma única página de capa. (ex: capa.extensão)")
            .Must(chapterValidatorService.ShouldHaveExactlyOneCreditsFile)
                .WithMessage("O diretório precisa conter apenas uma única página de créditos. (ex: creditos.extensão)")
            .Must(chapterValidatorService.ShouldHaveOnlySupportedFileExtensions)
                .WithMessage($"O diretório precisa conter apenas arquivos com as extensões esperadas: {string.Join("", FileReleaseService.ValidCoverFiles)}.")
            .Must(chapterValidatorService.ShouldHaveOrderedDoublePages)
                .WithMessage("As páginas duplas precisam estar numeradas em ordem e sequencialmente.")
            .Must(chapterValidatorService.ShouldHaveNotAnySkippedPage)
                .WithMessage("As páginas precisam ter números sequenciais, sem pular números.")
            .Must(chapterValidatorService.ShouldHaveSamePageLength)
                .WithMessage("O nome dos arquivos das páginas precisa ser escrito de modo que todos tenham o mesmo tamanho (dica: use zero à esqueda).")
            .Must(chapterValidatorService.ShouldStartInPageOne)
                .WithMessage("A primeira página deve começar com o número 1 (1, 01, 001...).")
            .Must(chapterValidatorService.ShouldNotHaveAnyTextPageThanCoverAndCredits)
                .WithMessage("Não deve conter outras páginas senão numerais, créditos e capa.");
}
