using System.Collections.Frozen;
using Righthand.RetroDbgDataProvider.Comparers;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;

namespace Righthand.RetroDbgDataProvider.Models;

public class ModifiableParsedFilesIndex<T>
    where T : ParsedSourceFile
{
    private readonly Dictionary<string, Dictionary<FrozenSet<string>, T>> _files = new();

    public bool ContainsKey(string fileName, FrozenSet<string> defineSymbols)
    {
        return _files.TryGetValue(fileName, out var files) && files.ContainsKey(defineSymbols);
    }    
    public bool TryAdd(string fileName, FrozenSet<string> defineSymbols, T file)
    {
        if (!_files.TryGetValue(fileName, out var files))
        {
            files = new Dictionary<FrozenSet<string>, T>(SetEqualityComparer<string>.Default);
            _files.Add(fileName, files);
        }

        return files.TryAdd(defineSymbols, file);
    }

    public ImmutableParsedFilesIndex<T> ToImmutable()
    {
        var builder = new Dictionary<string, IImmutableParsedFileSet<T>>(_files.Count);
        foreach (var item in _files)
        {
            var fileSet = new ImmutableParsedFileSet<T>(item.Value);
            builder.Add(item.Key, fileSet);
        }

        return new ImmutableParsedFilesIndex<T>(builder.ToFrozenDictionary());
    }
}