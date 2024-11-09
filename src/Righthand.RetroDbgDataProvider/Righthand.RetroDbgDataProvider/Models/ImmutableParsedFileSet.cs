using System.Collections.Frozen;
using Righthand.RetroDbgDataProvider.Comparers;

namespace Righthand.RetroDbgDataProvider.Models;

public interface IImmutableParsedFileSet<out T>
    where T: ParsedSourceFile
{
    T? GetFile(FrozenSet<string> defineSymbols);
    ImmutableArray<FrozenSet<string>> AllDefineSets { get; }
    int Count { get; }
}

public class ImmutableParsedFileSet<T> : IImmutableParsedFileSet<T>
    where T: ParsedSourceFile
{
    // ReSharper disable once InconsistentNaming
    private readonly FrozenDictionary<FrozenSet<string>, T> _files;

    public ImmutableArray<FrozenSet<string>> AllDefineSets => _files.Keys;
    public ImmutableParsedFileSet(IDictionary<FrozenSet<string>, T> files)
    {
        _files = files.ToFrozenDictionary(SetEqualityComparer<string>.Default);
    }
    public int Count => _files.Count;

    public T? GetFile(FrozenSet<string> defineSymbols) =>
        _files.GetValueOrDefault(defineSymbols);
}