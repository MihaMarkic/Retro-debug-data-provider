using System.Collections.Immutable;

namespace Righthand.RetroDbgDataProvider.Models.Program;
/// <summary>
/// Application block.
/// </summary>
/// <param name="Name">Name of the block.</param>
/// <param name="Items"></param>
public record Block(string Name, ImmutableArray<BlockItem> Items);
/// <summary>
/// An block item represents a memory range and its location in source file.
/// </summary>
/// <param name="Start">Start address</param>
/// <param name="End">End address</param>
/// <param name="FileLocation">Location in the source file.</param>
public record BlockItem(ushort Start, ushort End, MultiLineTextRange FileLocation);