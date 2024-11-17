using BotDeScans.App.Services;
using FluentValidation;
using Google.Apis.Drive.v3.Data;
using System.Globalization;
using File = Google.Apis.Drive.v3.Data.File;
namespace BotDeScans.App.Features.GoogleDrive;

public class FileListValidator : AbstractValidator<IList<File>>
{
    public FileListValidator() 
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(model => model)
            .Must(ShouldHaveOnlyFiles)
            .WithMessage("O diretório precisa conter apenas arquivos.");

        RuleFor(model => model)
            .Must(ShouldHaveExactlyOneCoverFile)
            .WithMessage("O diretório precisa conter apenas uma única página de capa. (ex: capa.extensão)");

        RuleFor(model => model)
            .Must(ShouldHaveExactlyOneCreditsFile)
            .WithMessage("O diretório precisa conter apenas uma única página de créditos. (ex: creditos.extensão)");

        RuleFor(model => model)
            .Must(ShouldHaveOnlySupportedFileExtensions)
            .WithMessage($"O diretório precisa conter apenas arquivos com as extensões esperadas: {string.Join("", FileReleaseService.ValidCoverFiles)}.");

        RuleFor(model => model)
            .Must(ShouldNotHaveAnyTextPageThanCoverAndCredits)
            .WithMessage("Não deve conter outras páginas senão numerais, créditos e capa.");

        RuleFor(model => model)
            .Must(ShouldStartInPageOne)
            .WithMessage("A primeira página deve começar com o número 1 (1, 01, 001...). Isso também vale para página dupla (1-2, 01-02, 001-002...).");

        RuleFor(model => model)
            .Must(ShouldHaveOrderedDoublePages)
            .WithMessage("As páginas duplas precisam estar numeradas em ordem e sequencialmente.");

        RuleFor(model => model)
            .Must(ShouldHaveNotAnySkippedPage)
            .WithMessage("As páginas precisam ter números sequenciais, sem pular números.");

        RuleFor(model => model)
            .Must(ShouldHaveSamePageLength)
            .WithMessage("O nome dos arquivos das páginas precisa ser escrito de modo que todos tenham o mesmo tamanho (dica: use zero à esqueda).");
    }

    private static bool ShouldHaveOnlyFiles(IList<File> files) =>
        files.All(x => x.Kind == "drive#file");

    private static bool ShouldHaveExactlyOneCoverFile(IList<File> files) =>
        files.Where(x => x.Kind == "drive#file")
             .Count(x => FileReleaseService.ValidCoverFiles.Contains(x.Name, StringComparer.InvariantCulture))
              == 1;

    private static bool ShouldHaveExactlyOneCreditsFile(IList<File> files) =>
        files.Where(x => x.Kind == "drive#file")
             .Count(x => FileReleaseService.ValidCreditsFiles.Contains(x.Name, StringComparer.InvariantCulture))
              == 1;

    private static bool ShouldHaveOnlySupportedFileExtensions(IList<File> files) =>
        files.Where(x => x.Kind == "drive#file")
             .All(x => FileReleaseService.ValidReleaseImageExtensions.Any(y => x
             .Name.EndsWith(y, StringComparison.InvariantCultureIgnoreCase)));

    private static bool ShouldNotHaveAnyTextPageThanCoverAndCredits(IList<File> files) =>
        files.Where(x => x.Kind == "drive#file")
             .Select(x => x.Name)
             .Where(x => !FileReleaseService.ValidCoverFiles.Contains(x, StringComparer.InvariantCultureIgnoreCase))
             .Where(x => !FileReleaseService.ValidCreditsFiles.Contains(x, StringComparer.InvariantCultureIgnoreCase))
             .Select(x => Path.GetFileNameWithoutExtension(x))
             .SelectMany(x => SplitDoublePages(x))
             .All(x => int.TryParse(x, out var _));

    private static bool ShouldStartInPageOne(IList<File> files) =>
        files.Where(x => x.Kind == "drive#file")
             .Select(x => x.Name)
             .Where(x => !FileReleaseService.ValidCoverFiles.Contains(x, StringComparer.InvariantCultureIgnoreCase))
             .Where(x => !FileReleaseService.ValidCreditsFiles.Contains(x, StringComparer.InvariantCultureIgnoreCase))
             .Select(x => Path.GetFileNameWithoutExtension(x))
             .SelectMany(x => SplitDoublePages(x))
             .Select(x => int.TryParse(x, out var intResult) ? intResult : int.MaxValue)
             .OrderBy(x => x)
             .First() == 1;

    private static bool ShouldHaveOrderedDoublePages(IList<File> files) =>
         files.Where(x => x.Kind == "drive#file")
              .Where(x => x.Name.Contains('-'))
              .Select(x => x.Name)
              .Where(x => !FileReleaseService.ValidCoverFiles.Contains(x, StringComparer.InvariantCultureIgnoreCase))
              .Where(x => !FileReleaseService.ValidCreditsFiles.Contains(x, StringComparer.InvariantCultureIgnoreCase))
              .Select(x => Path.GetFileNameWithoutExtension(x))
              .Select(x => SplitDoublePages(x).ToList())
              .All(x => x.Count == 2
                     && int.TryParse(x[0], out var firstValue)
                     && int.TryParse(x[1], out var lastValue)
                     && firstValue + 1 == lastValue);

    private static bool ShouldHaveNotAnySkippedPage(IList<File> files)
    {
        var filesNameAsNumber = FilesNameAsNumber(files);
        return filesNameAsNumber
            .Zip(filesNameAsNumber.Skip(1), (curr, next) => curr + 1 == next)
            .All(x => x);
    }

    private static bool ShouldHaveSamePageLength(IList<File> files) =>
        files.Where(x => x.Kind == "drive#file")
             .Select(x => x.Name)
             .Where(x => !FileReleaseService.ValidCoverFiles.Contains(x, StringComparer.InvariantCultureIgnoreCase))
             .Where(x => !FileReleaseService.ValidCreditsFiles.Contains(x, StringComparer.InvariantCultureIgnoreCase))
             .Select(x => Path.GetFileNameWithoutExtension(x))
             .SelectMany(x => SplitDoublePages(x))
             .GroupBy(x => x.Length)
             .Count() == 1;

    private static IOrderedEnumerable<int> FilesNameAsNumber(IList<File> files) =>
        files.Where(x => x.Kind == "drive#file")
             .Select(x => x.Name)
             .Where(x => !FileReleaseService.ValidCoverFiles.Contains(x, StringComparer.InvariantCultureIgnoreCase))
             .Where(x => !FileReleaseService.ValidCreditsFiles.Contains(x, StringComparer.InvariantCultureIgnoreCase))
             .Select(x => Path.GetFileNameWithoutExtension(x))
             .SelectMany(x => SplitDoublePages(x))
             .Select(x => int.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intResult)
                 ? intResult
                 : int.MaxValue)
             .Where(x => x != int.MaxValue)
             .OrderBy(x => x);

    private static string[] SplitDoublePages(string pageName)
    {
        var pages = pageName.Split('-');
        return pages.Length switch
        {
            1 => pages,
            2 => [pages[0], pages[1]],
            _ => ["error"],
        };
    }
}
