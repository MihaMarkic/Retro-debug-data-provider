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
    /// <summary>
    /// An empty file path.
    /// </summary>
    public static readonly SourceFilePath Empty = new SourceFilePath(string.Empty, IsRelative: true);
    /// <summary>
    /// Creates an instance of <see cref="SourceFilePath"/> with relative file path.
    /// </summary>
    /// <param name="relativePath">Relative path.</param>
    /// <returns>An instance of <see cref="SourceFilePath"/></returns>
    public static SourceFilePath CreateRelative(string relativePath) => new SourceFilePath(relativePath, true);
    /// <summary>
    /// Creates an instance of <see cref="SourceFilePath"/> with absolute file path.
    /// </summary>
    /// <param name="absolutePath">Absolute path.</param>
    /// <returns>An instance of <see cref="SourceFilePath"/></returns>
    public static SourceFilePath CreateAbsolute(string absolutePath) => new SourceFilePath(absolutePath, false);
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    /// <summary>
    /// File name part.
    /// </summary>
    public string FileName => System.IO.Path.GetFileName(Path);
    /// <summary>
    /// Directory part.
    /// </summary>
    public string? Directory => System.IO.Path.GetDirectoryName(Path);
    /// <summary>
    /// Creats an instance of <see cref="SourceFilePath"/>. If <paramref name="path"/> starts with <paramref name="directory"/>,
    /// then it assumes relative, absolute path otherwise.
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
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
    /// <summary>
    /// Compares two instances by value.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(SourceFilePath? other)
    {
        if (other is null)
        {
            return false;
        }
        var comparision = IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return other.IsRelative == IsRelative && string.Equals(other.Path, Path, comparision);
    }
    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(IsRelative, IsWindows ? Path.ToLowerInvariant() : Path);
    }
    /// <inheritdoc />
    public override string ToString()
    {
        string prefix = IsRelative ? "RELATIVE": "ABSOLUTE";
        return $"{prefix}:{Path}";
    }
}