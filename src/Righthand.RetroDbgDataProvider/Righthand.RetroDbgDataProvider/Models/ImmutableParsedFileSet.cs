using System.Collections.Frozen;
using Righthand.RetroDbgDataProvider.Comparers;

namespace Righthand.RetroDbgDataProvider.Models;

/// <summary>
/// Represents a map of parsed files grouped by define symbols.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IImmutableParsedFileSet<out T>
    where T: ParsedSourceFile
{
    /// <summary>
    /// Gets a parsed file with given define symbols set.
    /// </summary>
    /// <param name="defineSymbols"></param>
    /// <returns></returns>
    T? GetFile(FrozenSet<string> defineSymbols);
    /// <summary>
    /// Gets all define symbols sets.
    /// </summary>
    ImmutableArray<FrozenSet<string>> AllDefineSets { get; }
    /// <summary>
    /// Gets the number of define symbols sets and matching files.
    /// </summary>
    int Count { get; }
}
/// <inheritdoc />
public class ImmutableParsedFileSet<T> : IImmutableParsedFileSet<T>
    where T: ParsedSourceFile
{
    // ReSharper disable once InconsistentNaming
    private readonly FrozenDictionary<FrozenSet<string>, T> _files;
    /// <inheritdoc />
    public ImmutableArray<FrozenSet<string>> AllDefineSets => _files.Keys;
    /// <summary>
    /// Creates an instance of <see cref="ImmutableParsedFileSet{T}"/>.
    /// </summary>
    /// <param name="files"></param>
    public ImmutableParsedFileSet(IDictionary<FrozenSet<string>, T> files)
    {
        _files = files.ToFrozenDictionary(SetEqualityComparer<string>.Default);
    }
    /// <inheritdoc />
    public int Count => _files.Count;
    /// <inheritdoc />
    public T? GetFile(FrozenSet<string> defineSymbols) =>
        _files.GetValueOrDefault(defineSymbols);
}