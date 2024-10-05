namespace Righthand.RetroDbgDataProvider.Services.Abstract;

public interface IFileService
{
    bool FileExists(string path);
    Stream OpenRead(string path);
    DateTimeOffset GetLastWriteTime(string path);
}