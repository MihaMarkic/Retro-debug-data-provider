namespace Righthand.RetroDbgDataProvider.Services.Abstract;

/// <summary>
/// Provides file operations.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    bool FileExists(string path);
    /// <summary>
    /// Opens an existing file for reading.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    Stream OpenRead(string path);
    /// <summary>
    /// Reads text content from a file.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="options">Fixes line endings by default.</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<string> ReadAllTextAsync(string path, ReadAllTextOption options = ReadAllTextOption.FixLineEndings, CancellationToken ct = default);
    /// <summary>
    /// Gets last write time for given file defined with <paramref name="path"/>.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    DateTimeOffset GetLastWriteTime(string path);
    /// <summary>
    /// Writes content to file.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="text"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task WriteAllTextAsync(string path, string text, CancellationToken ct = default);
    /// <summary>
    /// Deletes a file.
    /// </summary>
    /// <param name="path"></param>
    void Delete(string path);
}

/// <summary>
/// Options for reading a text file.
/// </summary>
public enum ReadAllTextOption
{
    /// <summary>
    /// No particular option.
    /// </summary>
    None,
    /// <summary>
    /// Fixes line endings to the OS used.
    /// </summary>
    FixLineEndings,
}