using System.Collections;
using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.Models;
/// <inheritdoc />
public class ImmutableParsedFilesIndex<T> : IParsedFilesIndex<T>
    where T : ParsedSourceFile
{
    /// <summary>
    /// Gets an empty instance.
    /// </summary>
    public static readonly ImmutableParsedFilesIndex<T> Empty = new(FrozenDictionary<string, IImmutableParsedFileSet<T>>.Empty);
    private readonly FrozenDictionary<string, IImmutableParsedFileSet<T>> _data;
    /// <inheritdoc />
    public ImmutableArray<string> Keys => _data.Keys;
    /// <summary>
    /// Creates an instance of <see cref="ImmutableParsedFilesIndex{T}"/>.
    /// </summary>
    /// <param name="data"></param>
    public ImmutableParsedFilesIndex(FrozenDictionary<string, IImmutableParsedFileSet<T>> data)
    {
        _data = data;
    }
    /// <inheritdoc />
    public int Count => _data.Count;
    /// <inheritdoc />
    public IImmutableParsedFileSet<T> Get(string fileName) => _data[fileName];
    /// <inheritdoc />
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
    /// <inheritdoc />
    public IImmutableParsedFileSet<T>? GetValueOrDefault(string name) => _data.GetValueOrDefault(name);
    /// <inheritdoc />
    public T? GetFileOrDefault(string name, FrozenSet<string> defineSymbols)
    {
        return GetValueOrDefault(name)?.GetFile(defineSymbols);
    }
}
/// <summary>
/// Represents a parsed file and its variations based on define symobols sets.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IParsedFilesIndex<out T>: IEnumerable<IKeyValuePair<IImmutableParsedFileSet<T>>>
    where T : ParsedSourceFile
{
    /// <summary>
    /// Gets count of items.
    /// </summary>
    int Count { get; }
    /// <summary>
    /// Returns parsed file set based on given file name.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    IImmutableParsedFileSet<T>? GetValueOrDefault(string name);
    /// <summary>
    /// Returns parsed source file based on file name and define symbols set. 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="defineSymbols"></param>
    /// <returns></returns>
    T? GetFileOrDefault(string name, FrozenSet<string> defineSymbols);
    /// <summary>
    /// Gets all keys.
    /// </summary>
    ImmutableArray<string> Keys { get; }
}
/// <summary>
/// Covariant KeyValuePair equivalent.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IKeyValuePair<out T>
{
    /// <summary>
    /// Gets absolute path to file.
    /// </summary>
    public string AbsolutePath { get; }
    /// <summary>
    /// Gets the value associated with the file.
    /// </summary>
    public T Value { get; }
}
/// <inheritdoc />
public struct KeyValuePair<T> : IKeyValuePair<T>
{
    /// <inheritdoc />
    public string AbsolutePath { get; }
    /// <inheritdoc />
    public T Value { get; }
    /// <summary>
    /// Creates an instance of <see cref="KeyValuePair{T}"/>.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public KeyValuePair(string key, T value)
    {
        AbsolutePath = key;
        Value = value;
    }
}