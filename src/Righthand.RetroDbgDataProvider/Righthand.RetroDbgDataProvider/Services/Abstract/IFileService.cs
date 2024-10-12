namespace Righthand.RetroDbgDataProvider.Services.Abstract;

public interface IFileService
{
    bool FileExists(string path);
    Stream OpenRead(string path);
    Task<string> ReadAllTextAsync(string path, CancellationToken ct = default);
    DateTimeOffset GetLastWriteTime(string path);
}