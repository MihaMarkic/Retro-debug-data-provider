namespace Righthand.RetroDbgDataProvider.Services.Implementation;

public class FileService
{
    public bool FileExists(string path) => File.Exists(path);
    public Stream OpenRead(string path) => File.OpenRead(path);
    public DateTimeOffset GetLastWriteTime(string path) => File.GetLastWriteTime(path);
}