using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Righthand.RetroDbgDataProvider.Models.Program;

public record AssemblerAppInfo(
    FrozenDictionary<SourceFilePath, SourceFile> SourceFiles,
    ImmutableArray<Segment> Segments);