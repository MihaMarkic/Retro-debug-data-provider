using System.Collections.Immutable;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Models;

public record C64Debugger(
string Version,
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
public record Source(int Index, SourceOrigin Origin, string Path);
public record Segment(string Name, ImmutableArray<Block> Blocks);
public record Block(string Name, ImmutableArray<BlockItem> Items);
public record BlockItem(ushort Start, ushort End, FileLocation FileLocation);
public record Label(string SegmentName, ushort Address, string Name, int Start, int End,
    FileLocation FileLocation);
public record FileLocation(int SourceIndex, int Line1, int Col1,
    int Line2, int Col2);
public record Breakpoint(string SegmentName, ushort Address, string? Argument);
public record Watchpoint(string SegmentName, ushort Address1, ushort? Address2, string? Argument);
