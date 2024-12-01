namespace BotDeScans.App.Services;

/// <summary>
/// Manage release files and directories
/// </summary>
public class FileReleaseService : IDisposable
{
    public static readonly IEnumerable<string> ValidReleaseImageExtensions = ["jpg", "jpeg", "png"];
    private readonly string scopedDirectoryBaseName = Guid.NewGuid().ToString();
    private readonly List<string> scopedDirectories = [];

    // todo: podemos pensar sobre termos capas dinâmicas. Exemplo: Facebook usar capa-facebook, enquanto o blogger usar capa-blogger;
    // definirmos um nome de capa genérico, caso não ache... e uma configuração de obrigar/não obrigar nomes específicos,
    // além de deixar o usuário definir quais nomes. Podemos usar o app.config inicialmente. Configuração a nível de app, não de obra.
    public const string CoverPageName = "capa";
    public const string CreditPageName = "creditos";

    public static readonly IEnumerable<string> ValidCoverFiles =
        ValidReleaseImageExtensions.Select(x => $"{CoverPageName}.{x}");

    public static readonly IEnumerable<string> ValidCreditsFiles =
        ValidReleaseImageExtensions.Select(x => $"{CreditPageName}.{x}");

    /// <summary>
    /// Move single cover file to covers directory
    /// Use this method to assure that your release is not uploading together with a cover file.
    /// </summary>
    /// <returns>
    /// Cover file path.
    /// </returns>
    public virtual string MoveCoverFile(string releaseDirectory, string coverDirectory)
    {
        if (Directory.GetFiles(coverDirectory).Length != 0)
            throw new InvalidOperationException("Cover directory is not empty.");

        var coverFilePath = Directory
            .GetFiles(releaseDirectory)
            .Single(filePath => ValidCoverFiles.Any(coverName => filePath.EndsWith(coverName)));

        var coverFileNewPath = Path.Combine(coverDirectory, Path.GetFileName(coverFilePath));
        File.Move(coverFilePath, coverFileNewPath);

        return coverFileNewPath;
    }

    /// <summary>
    /// Creates a directory that will be deleted after class scope ends.
    /// </summary>
    /// <returns>
    /// The new scoped directory path.
    /// </returns>
    public virtual string CreateScopedDirectory()
    {
        var folderName = Guid.NewGuid().ToString();
        var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "release-status", scopedDirectoryBaseName, folderName);

        Directory.CreateDirectory(folderPath);
        scopedDirectories.Add(folderPath);

        return folderPath;
    }

    private bool disposed;
    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;

        foreach (var scopedDirectory in scopedDirectories)
            Directory.Delete(scopedDirectory, true);

        if (scopedDirectories.Count > 0)
        {
            var scopedDirectoryBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "release-status", scopedDirectoryBaseName);
            Directory.Delete(scopedDirectoryBasePath, true);
        }

        GC.SuppressFinalize(this);
    }
}
