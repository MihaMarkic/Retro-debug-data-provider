using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;
using static Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors.ArrayPropertyType;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static class ArrayProperties
{
    private static readonly FrozenDictionary<string, FrozenDictionary<string, ArrayProperty>> _data;

    static ArrayProperties()
    {
        _data = new Dictionary<string, HashSet<ArrayProperty>>
        {
            {
                ".file",
                [
                    new ArrayProperty("mbfiles", Bool),
                    new ArrayProperty("name", FileName),
                    new ArrayProperty("type", QuotedEnumerable, ["prg", "bin"])
                ]
            },
            {
                ".disk",
                [
                    new ArrayProperty("dontSplitFilesOverDir", Bool), new ArrayProperty("filename", FileName),
                    new ArrayProperty("format", QuotedEnumerable, ["commodore", "speddos", "dolphindos"]),
                    new ArrayProperty("id", Text),
                    new ArrayProperty("interleave", Number),
                    new ArrayProperty("name", Text),
                    new ArrayProperty("showInfo", Bool),
                    new ArrayProperty("storeFilesInDir", Bool),
                ]
            },
            {
                // special array of disk files
                ".DISK_FILE",
                [
                    new ArrayProperty("hide", Bool),
                    new ArrayProperty("interleave", Number),
                    new ArrayProperty("name", Text),
                    new ArrayProperty("noStartAddr", Bool),
                    new ArrayProperty("type", QuotedEnumerable, ["del", "seq", "prg", "usr", "rel", "del<", "seq<", "prg<", "usr<", "rel<"]),
                ]
            }
        }.ToFrozenDictionary(
            p => p.Key,
            p => p.Value.ToFrozenDictionary(v => v.Name));
    }

    public static bool GetProperty(string key, string propertyName, [NotNullWhen(true)] out ArrayProperty? property)
    {
        if (_data.TryGetValue(key, out var value))
        {
            return value.TryGetValue(propertyName, out property);
        }

        property = null;
        return false;
    }

    public static FrozenSet<string> GetNames(string key) =>
        _data.TryGetValue(key, out var result) ? result.Keys.ToFrozenSet() : FrozenSet<string>.Empty;

    public static FrozenSet<string> GetNames(string key, string root) =>
        _data.TryGetValue(key, out var result)
            ? result.Where(p => p.Key.StartsWith(root, StringComparison.Ordinal)).Select(p => p.Key).ToFrozenSet()
            : FrozenSet<string>.Empty;
}

public record ArrayProperty(string Name, ArrayPropertyType Type, FrozenSet<string>? Values = null);

public enum ArrayPropertyType
{
    Bool,
    Text,
    Number,
    TextArray,
    Enumerable,
    QuotedEnumerable,
    FileName,
}

public static class ArrayPropertyValues
{
    public static ImmutableArray<string> BoolValues { get; } = ["true", "false"];
}