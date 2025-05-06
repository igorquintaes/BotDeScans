namespace BotDeScans.App.Models.DTOs;

public sealed class FileChunk : IDisposable 
{
    private readonly Dictionary<string, FileStream> _files = [];

    public IReadOnlyDictionary<string, FileStream> Files => _files;

    public void Add(string fileName, FileStream stream) => _files.Add(fileName, stream);

    public int Count => _files.Count;

    public long TotalSize => _files.Values.Sum(fs => fs.Length);

    public void Dispose()
    {
        foreach (var stream in _files.Values)
            stream.Dispose();

        _files.Clear();
    }
}
