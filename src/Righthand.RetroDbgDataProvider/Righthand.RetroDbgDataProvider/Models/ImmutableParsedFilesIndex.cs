using System.Collections;
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

    public IEnumerator<IKeyValuePair<IImmutableParsedFileSet<T>>> GetEnumerator()
    {
        foreach (var p in _data)
        {
            yield return  new KeyValuePair<IImmutableParsedFileSet<T>>(p.Key, p.Value);
        }
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IImmutableParsedFileSet<T>? GetValueOrDefault(string name) => _data.GetValueOrDefault(name);

    public T? GetFileOrDefault(string name, FrozenSet<string> defineSymbols)
    {
        return GetValueOrDefault(name)?.GetFile(defineSymbols);
    }
}

public interface IParsedFilesIndex<out T>: IEnumerable<IKeyValuePair<IImmutableParsedFileSet<T>>>
    where T : ParsedSourceFile
{
    int Count { get; }
    IImmutableParsedFileSet<T>? GetValueOrDefault(string name);
    T? GetFileOrDefault(string name, FrozenSet<string> defineSymbols);
    ImmutableArray<string> Keys { get; }
}

public interface IKeyValuePair<out T>
{
    public string AbsolutePath { get; }
    public T Value { get; }
}
/// <summary>
/// Covariant KeyValuePair equivalent.
/// </summary>
/// <typeparam name="T"></typeparam>
public struct KeyValuePair<T> : IKeyValuePair<T>
{
    public string AbsolutePath { get; }
    public T Value { get; }

    public KeyValuePair(string key, T value)
    {
        AbsolutePath = key;
        Value = value;
    }
}