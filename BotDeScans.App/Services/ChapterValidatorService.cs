using Google.Apis.Drive.v3.Data;
using System.Globalization;
namespace BotDeScans.App.Services;

public class ChapterValidatorService
{
    public virtual bool ShouldHaveOnlyFiles(FileList fileList) =>
        fileList.Files
            .All(x => x.Kind == "drive#file");

    public virtual bool ShouldHaveExactlyOneCoverFile(FileList fileList) =>
        fileList.Files
            .Where(x => x.Kind == "drive#file")
            .Count(x => FileReleaseService.ValidCoverFiles.Contains(x.Name, StringComparer.InvariantCulture))
             == 1;

    public virtual bool ShouldHaveExactlyOneCreditsFile(FileList fileList) =>
        fileList.Files
            .Where(x => x.Kind == "drive#file")
            .Count(x => FileReleaseService.ValidCreditsFiles.Contains(x.Name, StringComparer.InvariantCulture))
             == 1;

    public virtual bool ShouldHaveOnlySupportedFileExtensions(FileList fileList) =>
        fileList.Files
            .Where(x => x.Kind == "drive#file")
            .All(x => FileReleaseService.ValidReleaseImageExtensions.Any(y => x
            .Name.EndsWith(y, StringComparison.InvariantCultureIgnoreCase)));

    public virtual bool ShouldHaveOrderedDoublePages(FileList fileList) =>
         fileList.Files
            .Where(x => x.Kind == "drive#file")
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

    public virtual bool ShouldHaveNotAnySkippedPage(FileList fileList)
    {
        var filesNameAsNumber = FilesNameAsNumber(fileList);
        return filesNameAsNumber
            .Zip(filesNameAsNumber.Skip(1), (curr, next) => curr + 1 == next)
            .All(x => x);
    }

    public virtual bool ShouldNotHaveAnyTextPageThanCoverAndCredits(FileList fileList) =>
        fileList.Files
            .Where(x => x.Kind == "drive#file")
            .Select(x => x.Name)
            .Where(x => !FileReleaseService.ValidCoverFiles.Contains(x, StringComparer.InvariantCultureIgnoreCase))
            .Where(x => !FileReleaseService.ValidCreditsFiles.Contains(x, StringComparer.InvariantCultureIgnoreCase))
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .SelectMany(x => SplitDoublePages(x))
            .All(x => int.TryParse(x, out var _));

    public virtual bool ShouldHaveSamePageLength(FileList fileList) =>
         fileList.Files
            .Where(x => x.Kind == "drive#file")
            .Select(x => x.Name)
            .Where(x => !FileReleaseService.ValidCoverFiles.Contains(x, StringComparer.InvariantCultureIgnoreCase))
            .Where(x => !FileReleaseService.ValidCreditsFiles.Contains(x, StringComparer.InvariantCultureIgnoreCase))
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .SelectMany(x => SplitDoublePages(x))
            .GroupBy(x => x.Length)
            .Count() == 1;

    public virtual bool ShouldStartInPageOne(FileList fileList) =>
         fileList.Files
            .Where(x => x.Kind == "drive#file")
            .Select(x => x.Name)
            .Where(x => !FileReleaseService.ValidCoverFiles.Contains(x, StringComparer.InvariantCultureIgnoreCase))
            .Where(x => !FileReleaseService.ValidCreditsFiles.Contains(x, StringComparer.InvariantCultureIgnoreCase))
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .SelectMany(x => SplitDoublePages(x))
            .Select(x => int.TryParse(x, out var intResult)
                ? intResult
                : int.MaxValue)
            .OrderBy(x => x)
            .First() == 1;

    private static IOrderedEnumerable<int> FilesNameAsNumber(FileList fileList) =>
        fileList.Files
            .Where(x => x.Kind == "drive#file")
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

    private static IEnumerable<string> SplitDoublePages(string pageName) =>
        pageName
            .Split('-')
            .Select(page => int.TryParse(page, out _)
                ? page : "error");
}
