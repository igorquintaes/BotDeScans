using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Services.Wrappers;

[ExcludeFromCodeCoverage]
public class StreamWrapper
{
    public virtual Stream CreateFileStream(string path, FileMode mode) =>
        new FileStream(path, mode);
}
