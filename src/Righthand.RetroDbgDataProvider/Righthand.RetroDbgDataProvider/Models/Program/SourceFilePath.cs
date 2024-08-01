using System.Runtime.InteropServices;

namespace Righthand.RetroDbgDataProvider.Models.Program;

/// <summary>
/// Defines both relative and absolute paths.
/// </summary>
/// <param name="Path"></param>
/// <param name="IsRelative"></param>
/// <remarks>On Windows path casing shouldn't matter.</remarks>
public sealed record SourceFilePath(string Path, bool IsRelative)
{
    public static readonly SourceFilePath Empty = new SourceFilePath(string.Empty, IsRelative: true);
    public static SourceFilePath CreateRelative(string relativePath) => new SourceFilePath(relativePath, true);
    public static SourceFilePath CreateAbsolute(string absolutePath) => new SourceFilePath(absolutePath, false);
    public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public string FileName => System.IO.Path.GetFileName(Path);
    public string? Directory => System.IO.Path.GetDirectoryName(Path);
    public static SourceFilePath Create(string directory, string path)
    {
        StringComparison comparison = IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var normalizedDirectory = System.IO.Path.GetFullPath(directory);
        var normalizedPath = System.IO.Path.GetFullPath(path);
        bool isRelative = normalizedPath.StartsWith(normalizedDirectory, comparison);
        if (isRelative)
        {
            // take care of last character, otherwise relative path might start with path separator
            int prefixLength = normalizedDirectory.EndsWith(System.IO.Path.PathSeparator)
                ? directory.Length : directory.Length + 1;
            if (normalizedPath.Length < prefixLength)
            {
                throw new Exception($"Path {normalizedPath} is shorter than prefix length of {prefixLength}");
            }
            // ReSharper disable once HeapView.ObjectAllocation
            return CreateRelative(normalizedPath[prefixLength..]);
        }
        else
        {
            return CreateAbsolute(path);
        }
    }
    public bool Equals(SourceFilePath? other)
    {
        if (other is null)
        {
            return false;
        }
        var comparision = IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return other.IsRelative == IsRelative && string.Equals(other.Path, Path, comparision);
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(IsRelative, IsWindows ? Path.ToLowerInvariant() : Path);
    }
    public override string ToString()
    {
        string prefix = IsRelative ? "RELATIVE": "ABSOLUTE";
        return $"{prefix}:{Path}";
    }
}