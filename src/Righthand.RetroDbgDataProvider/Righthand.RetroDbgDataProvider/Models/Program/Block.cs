using System.Collections.Immutable;

namespace Righthand.RetroDbgDataProvider.Models.Program;
public record Block(string Name, ImmutableArray<BlockItem> Items);
public record BlockItem(ushort Start, ushort End, TextRange FileLocation);