using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.Services.Abstract;

public interface IOSDependent
{
    StringComparison FileStringComparison { get; }
    StringComparer FileStringComparer { get; }
    string ViceExeName { get; }
    string JavaExeName { get; }
    string FileAppOpenName { get; }
    /// <summary>
    /// Normalizes path separator.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    string NormalizePath(string path);
    FrozenSet<string> ToFileFrozenSet(IList<string> files) => files.ToFrozenSet(FileStringComparer);
    Task<string> ReadAllTextAndAdjustLineEndingsAsync(Stream stream, CancellationToken ct = default);
}