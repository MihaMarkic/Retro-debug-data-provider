namespace Righthand.RetroDbgDataProvider.Services.Abstract;

public interface IFileService
{
    bool FileExists(string path) => File.Exists(path);
    Stream OpenRead(string path) => File.OpenRead(path);
}