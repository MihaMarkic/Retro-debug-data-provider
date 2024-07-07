using System.Collections.Immutable;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Models;

public record AssemblySegment(string Name, ImmutableArray<AssemblyBlock> Blocks);
public record AssemblyBlock(string Name, ImmutableArray<AssemblyLine> Lines);
public record AssemblyLine(ushort Address, ImmutableArray<byte> Data, string? Description);
