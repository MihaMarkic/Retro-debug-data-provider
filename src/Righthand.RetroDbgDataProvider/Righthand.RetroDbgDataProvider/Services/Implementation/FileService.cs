using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Services.Implementation;

public class FileService: IFileService
{
    public bool FileExists(string path) => File.Exists(path);
    public Stream OpenRead(string path) => File.OpenRead(path);
    public Task<string> ReadAllTextAsync(string path, CancellationToken ct = default) => File.ReadAllTextAsync(path, ct);
    public DateTimeOffset GetLastWriteTime(string path) => File.GetLastWriteTime(path);
}