namespace BotDeScans.App.Models.DTOs;

public sealed class FileChunk : IDisposable
{
    private readonly List<FileStream> _files = [];

    public IList<FileStream> Files => _files;

    public void Add(string fileName, FileStream stream) => _files.Add(stream);

    public int Count => _files.Count;

    public long TotalSize => _files.Sum(fs => fs.Length);

    public void Dispose()
    {
        foreach (var stream in _files)
            stream.Dispose();

        _files.Clear();
    }
}
