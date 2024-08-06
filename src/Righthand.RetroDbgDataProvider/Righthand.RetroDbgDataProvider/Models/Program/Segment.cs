using System.Collections.Immutable;

namespace Righthand.RetroDbgDataProvider.Models.Program;
/// <summary>
/// Application segment.
/// </summary>
/// <param name="Name">Segment name.</param>
/// <param name="Items">Block that belong to the segment.</param>
/// <param name="Breakpoints">A list of breakpoints.</param>
/// <param name="Watchpoints">A list of watchpoints.</param>
public record Segment(string Name, ImmutableArray<Block> Items, 
    ImmutableArray<Breakpoint> Breakpoints, ImmutableArray<Watchpoint> Watchpoints);