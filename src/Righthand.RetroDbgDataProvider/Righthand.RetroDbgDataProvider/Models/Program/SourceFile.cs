using System.Collections.Frozen;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;

namespace Righthand.RetroDbgDataProvider.Models.Program;

public record SourceFile(SourceFilePath Path,
    FrozenDictionary<TextRange, Label> Labels);