using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Righthand.RetroDbgDataProvider.Models.Program;

public record SourceFile(SourceFilePath Path,
    FrozenDictionary<string, Label> Labels,
    ImmutableArray<BlockItem> BlockItems);