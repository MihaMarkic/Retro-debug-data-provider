using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using static Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors.ArrayPropertyType;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static class ArrayProperties
{
    private static readonly FrozenDictionary<string, FrozenDictionary<string, ArrayProperty>> Data;

    static ArrayProperties()
    {
        Data = new Dictionary<string, HashSet<ArrayProperty>>
        {
            {
                ".segmentdef",
                [
                    new ArrayProperty("segments", Segments),
                ]
            },
            {
                ".file",
                [
                    new ArrayProperty("mbfiles", Bool),
                    new FileArrayProperty("name", FileName, [".prg"]),
                    new ValuesArrayProperty("type", QuotedEnumerable, ["prg", "bin"]),
                    new ArrayProperty("segments", Segments),
                ]
            },
            {
                ".disk",
                [
                    new ArrayProperty("dontSplitFilesOverDir", Bool), 
                    new FileArrayProperty("filename", FileName, ["*"]),
                    new ValuesArrayProperty("format", QuotedEnumerable, ["commodore", "speddos", "dolphindos"]),
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
                    new ValuesArrayProperty("type", QuotedEnumerable, ["del", "seq", "prg", "usr", "rel", "del<", "seq<", "prg<", "usr<", "rel<"]),
                ]
            }
        }.ToFrozenDictionary(
            p => p.Key,
            p => p.Value.ToFrozenDictionary(v => v.Name));
    }

    public static bool GetProperty(string key, string propertyName, [NotNullWhen(true)] out ArrayProperty? property)
    {
        if (Data.TryGetValue(key, out var value))
        {
            return value.TryGetValue(propertyName, out property);
        }

        property = null;
        return false;
    }

    public static FrozenSet<string> GetNames(string key) =>
        Data.TryGetValue(key, out var result) ? result.Keys.ToFrozenSet() : FrozenSet<string>.Empty;

    public static FrozenSet<string> GetNames(string key, string root) =>
        Data.TryGetValue(key, out var result)
            ? result.Where(p => p.Key.StartsWith(root, StringComparison.Ordinal) && !p.Key.Equals(root, StringComparison.Ordinal))
                .Select(p => p.Key).ToFrozenSet()
            : FrozenSet<string>.Empty;
}

public record ArrayProperty(string Name, ArrayPropertyType Type);

public record ValuesArrayProperty(string Name, ArrayPropertyType Type, FrozenSet<string>? Values = null) : ArrayProperty(Name, Type);

public record FileArrayProperty(string Name, ArrayPropertyType Type, FrozenSet<string> ValidExtensions) : ArrayProperty(Name, Type);

public enum ArrayPropertyType
{
    Bool,
    Text,
    Number,
    TextArray,
    Enumerable,
    QuotedEnumerable,
    FileName,
    FileNames,
    Segments,
}

public static class ArrayPropertyValues
{
    public static ImmutableArray<string> BoolValues { get; } = ["true", "false"];
}