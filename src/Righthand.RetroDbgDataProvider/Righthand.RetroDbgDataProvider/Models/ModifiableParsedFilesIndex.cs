using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Righthand.RetroDbgDataProvider.Comparers;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;

namespace Righthand.RetroDbgDataProvider.Models;

/// <summary>
/// Parsed file builder.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ModifiableParsedFilesIndex<T>
    where T : ParsedSourceFile
{
    private readonly Dictionary<string, Dictionary<FrozenSet<string>, T>> _files = new();
    /// <summary>
    /// Contains file references that are designated as #importonce
    /// </summary>
    private readonly Dictionary<string, (FrozenSet<string> DefineSymbols, T File)> _importOnceFiles = new();
    /// <summary>
    /// Determines whether the <see cref="ModifiableParsedFilesIndex{T}"/> contains the specified key.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="defineSymbols"></param>
    /// <returns></returns>
    public bool ContainsKey(string fileName, FrozenSet<string> defineSymbols)
    {
        return _files.TryGetValue(fileName, out var files) && files.ContainsKey(defineSymbols);
    }

    internal FrozenDictionary<string, (FrozenSet<string> DefineSymbols, T File)> ImportOnceFiles =>
        _importOnceFiles.ToFrozenDictionary();

    internal Dictionary<string, Dictionary<FrozenSet<string>, T>> Files => _files;

    /// <summary>
    /// Tries to store file with import once preprocessor directive.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="reference"></param>
    /// <returns>True when file name has been added, false otherwise.</returns>
    public bool TryGetImportOnce(string fileName, [NotNullWhen(true)]out (FrozenSet<string> DefineSymbols, T File)? reference)
    {
        if (_importOnceFiles.TryGetValue(fileName, out var tempReference))
        {
            reference = tempReference;
            return true;
        }

        reference = null;
        return false;
    }
    /// <summary>
    /// Adds file with import once preprocessor directive.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="defineSymbols"></param>
    /// <param name="file"></param>
    public void AddImportOnce(string fileName, FrozenSet<string> defineSymbols, T file)
    {
        _importOnceFiles.Add(fileName, (defineSymbols, file));
    }
    /// <summary>
    /// Attempts to add file.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="defineSymbols"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    public bool TryAdd(string fileName, FrozenSet<string> defineSymbols, T file)
    {
        if (!_files.TryGetValue(fileName, out var files))
        {
            files = new Dictionary<FrozenSet<string>, T>(SetEqualityComparer<string>.Default);
            _files.Add(fileName, files);
        }

        return files.TryAdd(defineSymbols, file);
    }

    /// <summary>
    /// Produces an immutable <see cref="ImmutableParsedFilesIndex{T}"/> of the content. 
    /// </summary>
    /// <returns></returns>
    public ImmutableParsedFilesIndex<T> ToImmutable()
    {
        // TODO check if we need to add importOnce files
        var builder = new Dictionary<string, IImmutableParsedFileSet<T>>(_files.Count);
        foreach (var item in _files)
        {
            var fileSet = new ImmutableParsedFileSet<T>(item.Value);
            builder.Add(item.Key, fileSet);
        }

        return new ImmutableParsedFilesIndex<T>(builder.ToFrozenDictionary());
    }
}