namespace Righthand.RetroDbgDataProvider.Services.Abstract;

public interface IFileService
{
    bool FileExists(string path);
    Stream OpenRead(string path);
    /// <summary>
    /// Reads text content from a file.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="options">Fixes line endings by default.</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<string> ReadAllTextAsync(string path, ReadAllTextOption options = ReadAllTextOption.FixLineEndings, CancellationToken ct = default);
    DateTimeOffset GetLastWriteTime(string path);
    Task WriteAllTextAsync(string path, string text, CancellationToken ct = default);
    void Delete(string path);
}

public enum ReadAllTextOption
{
    None,
    /// <summary>
    /// Fixes line endings to the OS used.
    /// </summary>
    FixLineEndings,
}