using System.Collections.Immutable;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Models;

/// <summary>
/// Assembly segments from byte dump.
/// </summary>
/// <param name="Name">Segment name</param>
/// <param name="Blocks">Children blocks</param>
public record AssemblySegment(string Name, ImmutableArray<AssemblyBlock> Blocks);
/// <summary>
/// Assembly block from byte dump.
/// </summary>
/// <param name="Name">Block name</param>
/// <param name="Lines">Children lines</param>
public record AssemblyBlock(string Name, ImmutableArray<AssemblyLine> Lines);
/// <summary>
/// Assembly line from block dump.
/// </summary>
/// <param name="Address">Line address</param>
/// <param name="Data">Line bytes</param>
/// <param name="Labels">Children labels</param>
/// <param name="Description">Line description</param>
public record AssemblyLine(ushort Address, ImmutableArray<byte> Data, ImmutableArray<string> Labels, string? Description);
