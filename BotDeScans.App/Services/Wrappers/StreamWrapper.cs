using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Services.Wrappers;

[ExcludeFromCodeCoverage]
public class StreamWrapper
{
    public virtual Stream CreateFileStream(string path, FileMode mode) =>
        new FileStream(path, mode);

    public virtual Stream CreateFileStream(string path, FileMode mode, FileShare share) =>
        new FileStream(path, mode, FileAccess.Read, share);
}
