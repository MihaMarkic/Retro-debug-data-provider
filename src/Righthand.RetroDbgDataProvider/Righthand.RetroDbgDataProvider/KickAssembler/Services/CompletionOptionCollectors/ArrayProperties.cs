using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using static Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors.ArrayPropertyType;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

/// <summary>
/// Contains data for directives array properties. 
/// </summary>
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

    /// <summary>
    /// Gets the property associated with the specified name.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="propertyName"></param>
    /// <param name="property"></param>
    /// <returns></returns>
    public static bool GetProperty(string key, string propertyName, [NotNullWhen(true)] out ArrayProperty? property)
    {
        if (Data.TryGetValue(key, out var value))
        {
            return value.TryGetValue(propertyName, out property);
        }

        property = null;
        return false;
    }

    /// <summary>
    /// Get array property names associated with directive with specified name.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static FrozenSet<string> GetNames(string key) =>
        Data.TryGetValue(key, out var result) ? result.Keys.ToFrozenSet() : FrozenSet<string>.Empty;

    /// <summary>
    /// Get array property values associated with a directive with specified name and starting with <paramref name="root"/>.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="root"></param>
    /// <returns></returns>
    public static FrozenSet<string> GetNames(string key, string root) =>
        Data.TryGetValue(key, out var result)
            ? result.Where(p => p.Key.StartsWith(root, StringComparison.Ordinal) && !p.Key.Equals(root, StringComparison.Ordinal))
                .Select(p => p.Key).ToFrozenSet()
            : FrozenSet<string>.Empty;
}
/// <summary>
/// Generic array property.
/// </summary>
/// <param name="Name"></param>
/// <param name="Type"></param>
public record ArrayProperty(string Name, ArrayPropertyType Type);
/// <summary>
/// An array property with finite values.
/// </summary>
/// <param name="Name"></param>
/// <param name="Type"></param>
/// <param name="Values"></param>
public record ValuesArrayProperty(string Name, ArrayPropertyType Type, FrozenSet<string>? Values = null) : ArrayProperty(Name, Type);
/// <summary>
/// An array property representing a file reference.
/// </summary>
/// <param name="Name"></param>
/// <param name="Type"></param>
/// <param name="ValidExtensions"></param>
public record FileArrayProperty(string Name, ArrayPropertyType Type, FrozenSet<string> ValidExtensions) : ArrayProperty(Name, Type);
/// <summary>
/// Represents an array property type.
/// </summary>
public enum ArrayPropertyType
{
    /// <summary>
    /// A boolean.
    /// </summary>
    Bool,
    /// <summary>
    /// A text.
    /// </summary>
    Text,
    /// <summary>
    /// A number.
    /// </summary>
    Number,
    /// <summary>
    /// An array of string values.
    /// </summary>
    TextArray,
    /// <summary>
    /// An enum.
    /// </summary>
    Enumerable,
    /// <summary>
    /// An enum with quoted values.
    /// </summary>
    QuotedEnumerable,
    /// <summary>
    /// A file name reference.
    /// </summary>
    FileName,
    /// <summary>
    /// Reference to multiple file names.
    /// </summary>
    FileNames,
    /// <summary>
    /// Segments.
    /// </summary>
    Segments,
}

/// <summary>
/// Represents constant array property values.
/// </summary>
public static class ArrayPropertyValues
{
    /// <summary>
    /// A set of boolean values.
    /// </summary>
    public static ImmutableArray<string> BoolValues { get; } = ["true", "false"];
}