using System.Collections.Immutable;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Models;

public record DbgData(
    string Version,
    string Path,
    ImmutableArray<Source> Sources,
    ImmutableArray<Segment> Segments,
    ImmutableArray<Label> Labels,
    ImmutableArray<Breakpoint> Breakpoints,
    ImmutableArray<Watchpoint> Watchpoints);

public enum SourceOrigin
{
    KickAss,
    User
}

/// <summary>
/// Represents source file.
/// </summary>
/// <param name="Index"></param>
/// <param name="Origin">File origin - it can be user or internal KickAssembler</param>
/// <param name="FullPath">Full path to file when <param name="Origin"></param> is <see cref="SourceOrigin.User"/> otherwise relative</param>
public record Source(int Index, SourceOrigin Origin, string FullPath)
{
    /// <summary>
    /// Returns relative path to <paramref name="projectDirectory"/>.
    /// </summary>
    /// <param name="projectDirectory"></param>
    /// <returns></returns>
    public string? GetRelativePath(string projectDirectory)
    {
        if (FullPath.StartsWith(projectDirectory))
        {
            return FullPath.Substring(projectDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
        }

        return null;
    }
}
public record Segment(string Name, ImmutableArray<Block> Blocks);
public record Block(string Name, ImmutableArray<BlockItem> Items);
public record BlockItem(ushort Start, ushort End, FileLocation FileLocation);
public record Label(string SegmentName, ushort Address, string Name, FileLocation FileLocation);
public record FileLocation(int SourceIndex, int Line1, int Col1,
    int Line2, int Col2);
public record Breakpoint(string SegmentName, ushort Address, string? Argument);
public record Watchpoint(string SegmentName, ushort Address1, ushort? Address2, string? Argument);
