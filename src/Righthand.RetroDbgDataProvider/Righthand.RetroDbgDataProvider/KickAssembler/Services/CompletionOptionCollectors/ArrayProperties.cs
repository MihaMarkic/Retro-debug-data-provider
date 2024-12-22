using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static class ArrayProperties
{
    private static readonly FrozenDictionary<string, FrozenSet<string>> _data;

    static ArrayProperties()
    {
        _data = new Dictionary<string, HashSet<string>>
        {
            { ".file", ["mbfiles", "name", "type"] }
        }.ToFrozenDictionary(p => p.Key, p => p.Value.ToFrozenSet());
    }

    public static FrozenSet<string> GetNames(string key) =>
        _data.TryGetValue(key, out var result) ? result : FrozenSet<string>.Empty;

    public static FrozenSet<string> GetNames(string key, string root) =>
        _data.TryGetValue(key, out var result)
            ? result.Where(v => v.StartsWith(root, StringComparison.Ordinal)).ToFrozenSet()
            : FrozenSet<string>.Empty;
}