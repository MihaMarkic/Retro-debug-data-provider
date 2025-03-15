using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.Services.Abstract;

/// <summary>
/// Represents types that provide OS dependent code. 
/// </summary>
public interface IOSDependent
{
    /// <summary>
    /// A <see cref="FileStringComparison"/> instance.
    /// </summary>
    StringComparison FileStringComparison { get; }
    /// <summary>
    /// A <see cref="FileStringComparer"/> instance.
    /// </summary>
    StringComparer FileStringComparer { get; }
    /// <summary>
    /// Vice executable name.
    /// </summary>
    string ViceExeName { get; }
    /// <summary>
    /// Java compiler executable name.
    /// </summary>
    string JavaExeName { get; }
    /// <summary>
    /// Verb to open any file with associated application.
    /// </summary>
    string FileAppOpenName { get; }
    /// <summary>
    /// Normalizes path separator.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    string NormalizePath(string path);
    /// <summary>
    /// Converts a list of file names to a <see cref="FileStringComparer"/> ordered set.
    /// </summary>
    /// <param name="files"></param>
    /// <returns></returns>
    FrozenSet<string> ToFileFrozenSet(IList<string> files) => files.ToFrozenSet(FileStringComparer);
    /// <summary>
    /// Reads a text file and adjusts line endings.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<string> ReadAllTextAndAdjustLineEndingsAsync(Stream stream, CancellationToken ct = default);
}