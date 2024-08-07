using System.Collections.Immutable;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Models;

/// <summary>
/// Structure representing Kick Assembler's .dbg file
/// </summary>
/// <param name="Version"></param>
/// <param name="Path"></param>
/// <param name="Sources"></param>
/// <param name="Segments"></param>
/// <param name="Labels"></param>
/// <param name="Breakpoints"></param>
/// <param name="Watchpoints"></param>
public record DbgData(
    string Version,
    string Path,
    ImmutableArray<Source> Sources,
    ImmutableArray<Segment> Segments,
    ImmutableArray<Label> Labels,
    ImmutableArray<Breakpoint> Breakpoints,
    ImmutableArray<Watchpoint> Watchpoints);

/// <summary>
/// Source file origin.
/// </summary>
public enum SourceOrigin
{
    /// <summary>
    /// This is a Kick Assembler .asm file.
    /// </summary>
    KickAss,
    /// <summary>
    /// Unknown file.
    /// </summary>
    User
}

/// <summary>
/// Represents source file.
/// </summary>
/// <param name="Index"></param>
/// <param name="Origin">File origin - it can be user or internal KickAssembler</param>
/// <param name="FullPath">Full path to file when <paramref name="Origin" /> is <see cref="SourceOrigin.User"/> otherwise relative</param>
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
            return FullPath[projectDirectory.Length..].TrimStart(Path.DirectorySeparatorChar);
        }

        return null;
    }
}
/// <summary>
/// Segment.
/// </summary>
/// <param name="Name"></param>
/// <param name="Blocks"></param>
public record Segment(string Name, ImmutableArray<Block> Blocks);
/// <summary>
/// Block
/// </summary>
/// <param name="Name"></param>
/// <param name="Items"></param>
public record Block(string Name, ImmutableArray<BlockItem> Items);
/// <summary>
/// Block item.
/// </summary>
/// <param name="Start"></param>
/// <param name="End"></param>
/// <param name="FileLocation"></param>
public record BlockItem(ushort Start, ushort End, FileLocation FileLocation);
/// <summary>
/// Label.
/// </summary>
/// <param name="SegmentName"></param>
/// <param name="Address"></param>
/// <param name="Name"></param>
/// <param name="FileLocation"></param>
public record Label(string SegmentName, ushort Address, string Name, FileLocation FileLocation);
/// <summary>
/// File location. A text range and file containing it.
/// </summary>
/// <param name="SourceIndex">Index of the file in <see cref="DbgData.Sources"/></param>
/// <param name="Line1">Starting line</param>
/// <param name="Col1">Starting column within starting line</param>
/// <param name="Line2">Ending line</param>
/// <param name="Col2">Ending column within ending line</param>
public record FileLocation(int SourceIndex, int Line1, int Col1,
    int Line2, int Col2);
/// <summary>
/// Breakpoint. These are hardcoded in source files.
/// </summary>
/// <param name="SegmentName">Parent segment</param>
/// <param name="Address">Memory address</param>
/// <param name="Argument">Optional arguments - syntax depends on emulator</param>
public record Breakpoint(string SegmentName, ushort Address, string? Argument);
/// <summary>
/// Watchpoint. These are hardcode in source files.
/// </summary>
/// <param name="SegmentName">Parent segment</param>
/// <param name="Address1">Starting memory address</param>
/// <param name="Address2">Ending memory address</param>
/// <param name="Argument">Optional arguments - syntax depends on emulator</param>
public record Watchpoint(string SegmentName, ushort Address1, ushort? Address2, string? Argument);
