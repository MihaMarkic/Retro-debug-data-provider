using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.Models;

public class ImmutableParsedFilesIndex<T> : IParsedFilesIndex<T>
    where T : ParsedSourceFile
{
    public static readonly ImmutableParsedFilesIndex<T> Empty = new(FrozenDictionary<string, IImmutableParsedFileSet<T>>.Empty);
    private readonly FrozenDictionary<string, IImmutableParsedFileSet<T>> _data;
    public ImmutableArray<string> Keys => _data.Keys;
    public ImmutableParsedFilesIndex(FrozenDictionary<string, IImmutableParsedFileSet<T>> data)
    {
        _data = data;
    }

    public int Count => _data.Count;
    public IImmutableParsedFileSet<T> Get(string fileName) => _data[fileName];

    public IEnumerable<KeyValuePair<string, IImmutableParsedFileSet<T>>> GetEnumerator()
    {
        foreach (var p in _data)
        {
            yield return p;
        }
    }

    public IImmutableParsedFileSet<T>? GetValueOrDefault(string name) => _data.GetValueOrDefault(name);

    public T? GetFileOrDefault(string name, FrozenSet<string> defineSymbols)
    {
        return GetValueOrDefault(name)?.GetFile(defineSymbols);
    }
}

public interface IParsedFilesIndex<out T>
    where T : ParsedSourceFile
{
    int Count { get; }
    IImmutableParsedFileSet<T>? GetValueOrDefault(string name);
    T? GetFileOrDefault(string name, FrozenSet<string> defineSymbols);
    ImmutableArray<string> Keys { get; }
}