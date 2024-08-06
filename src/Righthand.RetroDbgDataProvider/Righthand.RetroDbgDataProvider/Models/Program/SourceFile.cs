using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Righthand.RetroDbgDataProvider.Models.Program;
/// <summary>
/// Source file.
/// </summary>
/// <param name="Path">File path.</param>
/// <param name="Labels">List of labels.</param>
/// <param name="BlockItems">List of block items.</param>
public record SourceFile(SourceFilePath Path,
    FrozenDictionary<string, Label> Labels,
    ImmutableArray<BlockItem> BlockItems);